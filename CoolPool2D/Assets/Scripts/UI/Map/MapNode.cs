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
    private const string PATH_COLOUR = "#BBBBC5";
    private const float LINE_WIDTH = .08f;
    private const int AMOUNT_OF_POINTS = 2;   
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
        lineRenderer.positionCount = AMOUNT_OF_POINTS;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = LINE_WIDTH;
        lineRenderer.endWidth = LINE_WIDTH;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        Color pathColour;
        ColorUtility.TryParseHtmlString(PATH_COLOUR, out pathColour);
        lineRenderer.startColor = pathColour;
        lineRenderer.endColor = pathColour;
    }
}
