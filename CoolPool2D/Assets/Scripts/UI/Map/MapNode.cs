using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
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

        transform.position = new Vector3(x, y, 0);

        SetSprite();
        AddPolygonCollider();

        ConfigureLineRenderer();
        DrawPathsToNextNodes();
    }

    private void SetSprite()
    {
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = type switch
        {
            MapNodeType.Start => Resources.Load<Sprite>("Sprites/Start"),
            MapNodeType.Treasure => Resources.Load<Sprite>("Sprites/Treasure"),
            MapNodeType.PoolEncounter => Resources.Load<Sprite>("Sprites/RedDragonSign"),
            MapNodeType.Shop => Resources.Load<Sprite>("Sprites/ShabbyCloth"),
            MapNodeType.RandomEvent => Resources.Load<Sprite>("Sprites/PaddysPub"),
            _ => null,
        };
        SetColour(spriteRenderer);
    }

    private void AddPolygonCollider()
    {
        // Remove any existing collider to avoid duplicates
        var existing = gameObject.GetComponent<PolygonCollider2D>();
        if (existing != null)
            Destroy(existing);

        var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        var sprite = spriteRenderer.sprite;
        if (sprite == null) return;

        var collider = gameObject.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true; // Optional: set as trigger if you don't want physics

        // Set collider shape to match sprite
        collider.pathCount = sprite.GetPhysicsShapeCount();
        List<Vector2> path = new List<Vector2>();
        for (int i = 0; i < collider.pathCount; i++)
        {
            path.Clear();
            sprite.GetPhysicsShape(i, path);
            collider.SetPath(i, path.ToArray());
        }
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

    public void DrawPathsToNextNodes()
    {
        bool isFirstNode = true;
        foreach (var nextNode in Next)
        {
            if (!isFirstNode)
            {
                lineRenderer = Instantiate(lineRenderer, transform);
            }
            lineRenderer.SetPosition(0, new Vector3(x, y, 0));
            lineRenderer.SetPosition(1, new Vector3(nextNode.x, nextNode.y, 0));
            isFirstNode = false;
        }
    }

    private void SetColour(SpriteRenderer spriteRenderer = null)
    {
        if (spriteRenderer == null) spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        var dataManagerCurrentNode = DataManager.Instance.Data.MapData.CurrentNode;
        if (dataManagerCurrentNode != null && dataManagerCurrentNode.Coordinates.x == x &&
            dataManagerCurrentNode.Coordinates.y == y)
        {
            spriteRenderer.color = Color.yellow;
        }
        else if (!IsTraversable())
        {
            spriteRenderer.color = Color.red;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    private void OnMouseDown()
    {
        if (type == null) return;
        if (IsTraversable())
        {
            var traversalNode = DataManager.Instance.Data.MapData.GeneratedMap.Find(node => node.Coordinates.x == x && node.Coordinates.y == y);
            DataManager.Instance.Data.MapData.CurrentNode = traversalNode;
            DataManager.Instance.SaveData();
            Debug.Log($"type:{type} X:{x} Y:{y} clicked on traversable node");
        }
        GameObject.FindGameObjectsWithTag("MapNode").ToList().ForEach(node => node.GetComponent<MapNode>().SetColour());    
    }

    private bool IsTraversable()
    {
        if (type == MapNodeType.Start && DataManager.Instance.Data.MapData.CurrentNode == null) return true;
        if (DataManager.Instance.Data.MapData.CurrentNode == null) return false;
        if (DataManager.Instance.Data.MapData.CurrentNode.Next.Any(node => node.x == x && node.y == y)) return true;
        return false;
    }
}
