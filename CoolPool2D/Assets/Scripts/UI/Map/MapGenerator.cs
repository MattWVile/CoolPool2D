using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


public class MapGenerator : MonoBehaviour
{
    public List<MapNode> AllNodes;

    void Start()
    {
        AllNodes = new List<MapNode>();
        CreateNodes();
        CreateNodePath();
        CreateNodePath();
        CreateNodePath();
        CreateNodePath();

        AllNodes.ForEach(node =>
        {
            if (string.IsNullOrEmpty(node.type)) return;
            var nodeGameObject = Instantiate(Resources.Load("Prefabs/MapNode"));
            nodeGameObject.GetComponent<MapNode>().Instantiate(node);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CreateNodes()
    {

        for (var x = 0; x < 15; x++) {
            for (var y = 0; y < 7; y++) {
                AllNodes.Add(new MapNode() { x = x, y = y });
            }
        }

    }

    private MapNode GetRandomStarterNode()
    {
        var allFirstNodes = AllNodes.Where(node => node.x == 0).ToList();
        var allFirstNodesWithoutNext = allFirstNodes.Where(node => node.Next.Count == 0).ToList();
        var randomFirstNode = allFirstNodesWithoutNext.OrderBy(x => Random.Range(0, 20)).First();
        return randomFirstNode;
    }

    private MapNode GetNextNodeFor(MapNode currentNode)
    {
        MapNode nextNodeInPath;

        var nodesInNextColumn = AllNodes.Where(node => node.x == currentNode.x + 1).ToList();
        if (!nodesInNextColumn.Any()) // if there are no next nodes
            throw new IndexOutOfRangeException();

        if (currentNode.Prev.Count > 1) { // if current node has more than 1 previous node, we must find a different next node

            var reachableNodesInNextColumn = nodesInNextColumn.Where(node =>
                node.y != currentNode.y - 1 ||
                node.y != currentNode.y ||
                node.y != currentNode.y + 1
            ).ToList();

            var existingPathYValues = currentNode.Next.Select(prevNode => prevNode.y).ToList();
            var possibleNextNodes = reachableNodesInNextColumn.Where(node => !existingPathYValues.Contains(node.y));

            nextNodeInPath = Random.Range(0, 2) == 0 // coinflip
                ? possibleNextNodes.First()
                : possibleNextNodes.Last();

            return nextNodeInPath;
        }

        var nextY = GetRandomNextY(currentNode.y);
        nextNodeInPath = nodesInNextColumn.First(node => node.y == nextY);
        return nextNodeInPath;
    }

    public void CreateNodePath() {

        var currNode = GetRandomStarterNode();
        currNode.type = "start";

        while (true)
        {
            MapNode nextPathNode;
            try
            {
                nextPathNode = GetNextNodeFor(currNode);
            }
            catch (IndexOutOfRangeException)
            {
                break;
            }
            currNode.Next.Add(nextPathNode);
            currNode = nextPathNode;
            currNode.type = "pathNode";
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
