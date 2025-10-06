using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VirtualMapNode
{
    public MapNodeType type;
    public List<Coordinates> Prev;
    public List<Coordinates> Next;
    public Coordinates Coordinates;
}

[Serializable]
public class Coordinates
{
    public int x;
    public int y;
}

public class MapNode : MonoBehaviour
{
    public MapNodeType? type { get; set; }

    public List<Coordinates> Prev = new();
    public List<Coordinates> Next = new();
    public int x { get; set; }
    public int y { get; set; }

    private LineRenderer lineRenderer;
    private string pathColourHex = "#BBBBC5";
    public void Instantiate(VirtualMapNode node)
    {
        type = node.type;
        x = node.Coordinates.x;
        y = node.Coordinates.y;
        Prev = node.Prev;
        Next = node.Next;

        transform.position = new Vector3(x , y , 0);

        ConfigureLineRenderer();
        SetSprite();
        DrawPathsToNextNodes();
    }

    public void DrawPathsToNextNodes()
    {
        foreach (var nextNode in Next)
        {
            lineRenderer = Instantiate(lineRenderer, transform);
            lineRenderer.SetPosition(0, new Vector3(x, y, 0));
            lineRenderer.SetPosition(1, new Vector3(nextNode.x, nextNode.y, 0));
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

    private void ConfigureLineRenderer()
    {
        lineRenderer = new GameObject("LineRenderer").AddComponent<LineRenderer>();
        lineRenderer.transform.parent = transform;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        Color pathColour;
        ColorUtility.TryParseHtmlString(pathColourHex, out pathColour);
        lineRenderer.startColor = pathColour;
        lineRenderer.endColor = pathColour;
    }
}
