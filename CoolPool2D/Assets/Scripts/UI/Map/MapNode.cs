using System.Collections.Generic;
using UnityEngine;

public class MapNode : MonoBehaviour
{
    public MapNodeType? type { get; set; }

    public List<MapNode> Prev = new();
    public List<MapNode> Next = new();
    public int x { get; set; }
    public int y { get; set; }

    public void Instantiate(MapNode node)
    {
        type = node.type;
        x = node.x;
        y = node.y;
        Prev = node.Prev;
        Next = node.Next;

        transform.position = new Vector3(x , y , 0);
        SetSprite();
        DrawPathsToNextNodes();
    }

    public void DrawPathsToNextNodes()
    {
        foreach (var nextNode in Next)
        {
            Debug.DrawLine(new Vector3(x, y, 0), new Vector3(nextNode.x, nextNode.y, 0), Color.red, 100f);
        }
    }

    private void SetSprite()
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = type switch
        {
            MapNodeType.Start => Resources.Load<Sprite>("Sprites/Start"),
            MapNodeType.Treasure => Resources.Load<Sprite>("Sprites/Treasure"),
            MapNodeType.PoolEncounter => Resources.Load<Sprite>("Sprites/RedDragonSign"),
            MapNodeType.Shop => Resources.Load<Sprite>("Sprites/ShabbyCloth"),
            MapNodeType.RandomEvent => Resources.Load<Sprite>("Sprites/PaddysPub"),
        };
    }
}
