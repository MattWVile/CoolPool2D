using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    public List<VirtualMapNode> AllNodes;

    void Start()
    {
        var MapFromSaveData = DataManager.Instance.Data.MapData.GeneratedMap;
        if (MapFromSaveData != null && MapFromSaveData.Count != 0)
        {
            AllNodes = MapFromSaveData;
        }
        else
        {
            GenerateMap();
            DataManager.Instance.Data.MapData = new MapData() {
                GeneratedMap = AllNodes,
                CurrentNode = null
            };
            DataManager.Instance.SaveData();
        }

        VisualizeMap();

    }

    private void GenerateMap()
    {
        AllNodes = new List<VirtualMapNode>();
        CreateNodes();
        CreateNodePath();
        CreateNodePath();
        CreateNodePath();
        CreateNodePath();
        // TODO: ValidateMap();
        // Check map validity
        // (2 shops in a row, no nodes of certain type, path too intertwined etc)
        PopulateNodes();
    }

    private void VisualizeMap()
    {
        foreach (var virtualNode in AllNodes.FindAll(node => node.type != MapNodeType.Empty))
        {
            var nodeGameObject = Instantiate(Resources.Load("Prefabs/MapNode"));
            nodeGameObject.GetComponent<MapNode>().Instantiate(virtualNode);
        }
    }

    private void PopulateNodes()
    {
        foreach (var node in AllNodes.FindAll(node => node.type == MapNodeType.PartOfPath))
        {
            node.type = GetNodeType(node);
        }

    }

    private MapNodeType GetNodeType(VirtualMapNode currentNode)
    {
        if (currentNode.Coordinates.x == 0)
            return MapNodeType.Start;
        if (currentNode.Coordinates.x == 5) return MapNodeType.Treasure;

        if (currentNode.Coordinates.x < 5 && currentNode.Coordinates.x > 2) 
        {
            var randomValue = Random.Range(0, 10);
            if (randomValue < 2) return MapNodeType.Shop;
            if (randomValue < 5) return MapNodeType.RandomEvent;
            return MapNodeType.PoolEncounter;
        }

        if (currentNode.Coordinates.x < 12 && currentNode.Coordinates.x > 9) 
        {
            var randomValue = Random.Range(0, 10);
            if (randomValue < 2) return MapNodeType.Shop;
            if (randomValue < 4) return MapNodeType.RandomEvent;
            return MapNodeType.PoolEncounter;
        }
        var randomValue2 = Random.Range(0, 100);

        if (randomValue2 <= 5) return MapNodeType.Treasure;
        if (randomValue2 > 5 && randomValue2 <= 15) return MapNodeType.RandomEvent;

        return MapNodeType.PoolEncounter;
    }

    private void CreateNodes()
    {

        for (var x = 0; x < 15; x++) {
            for (var y = 0; y < 7; y++) {
                AllNodes.Add(new VirtualMapNode()
                {
                    Coordinates = new Coordinates { x = x, y = y },
                    type = MapNodeType.Empty,
                    Next = new List<Coordinates>(),
                    Prev = new List<Coordinates>()
                });
            }
        }

    }

    private VirtualMapNode GetRandomStarterNode()
    {
        var allFirstNodes = AllNodes.Where(node => node.Coordinates.x == 0).ToList();
        var allFirstNodesWithoutNext = allFirstNodes.Where(node => node.Next.Count == 0).ToList();
        var randomFirstNode = allFirstNodesWithoutNext.OrderBy(x => Random.Range(0, 20)).First();
        randomFirstNode.type = MapNodeType.Start;
        return randomFirstNode;
    }

    private VirtualMapNode GetNextNodeFor(VirtualMapNode currentNode)
    {
        VirtualMapNode nextNodeInPath;

        var nodesInNextColumn = AllNodes.Where(node => node.Coordinates.x == currentNode.Coordinates.x + 1).ToList();
        if (!nodesInNextColumn.Any()) // if there are no next nodes
            throw new IndexOutOfRangeException();

        if (currentNode.Prev.Count > 1) { // if current node has more than 1 previous node, we must find a different next node

            var reachableNodesInNextColumn = nodesInNextColumn.Where(node =>
                node.Coordinates.y != currentNode.Coordinates.y - 1 ||
                node.Coordinates.y != currentNode.Coordinates.y ||
                node.Coordinates.y != currentNode.Coordinates.y + 1
            ).ToList();

            var existingPathYValues = currentNode.Next.Select(prevNode => prevNode.y).ToList();
            var possibleNextNodes = reachableNodesInNextColumn.Where(node => !existingPathYValues.Contains(node.Coordinates.y));

            nextNodeInPath = Random.Range(0, 2) == 0 // coinflip
                ? possibleNextNodes.First()
                : possibleNextNodes.Last();

            return nextNodeInPath;
        }

        var nextY = GetRandomNextY(currentNode.Coordinates.y);
        nextNodeInPath = nodesInNextColumn.First(node => node.Coordinates.y == nextY);
        return nextNodeInPath;
    }

    public void CreateNodePath() {

        var currentNode = GetRandomStarterNode();

        while (true)
        {
            VirtualMapNode nextPathNode;
            try
            {
                nextPathNode = GetNextNodeFor(currentNode);
            }
            catch (IndexOutOfRangeException)
            {
                break;
            }
            currentNode.Next.Add(nextPathNode.Coordinates);
            currentNode = nextPathNode;
            currentNode.type = MapNodeType.PartOfPath;
        }
    }

    public int GetRandomNextY(int currentY) {
        int random = Random.Range(-1, 2);
        int nextY = currentY + random;

        if (nextY < 0)
            return 0;
        if (nextY > 6)
            return 6;

        return nextY;
    }
}
