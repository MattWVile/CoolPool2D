using System;
using System.Collections;
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
    private const float LINE_WIDTH = .1f;
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
        lineRenderer.material = new Material(Resources.Load<Material>("Sprites/Materials/CobbledStreetMaterial"));
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.textureScale = new Vector2(10f, 1.5f);
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = -1; // lower number = drawn behind
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
            StartCoroutine(TraverseToNode(traversalNode));
        }
        GameObject.FindGameObjectsWithTag("MapNode").ToList().ForEach(node => node.GetComponent<MapNode>().SetColour());    
    }

    public bool IsTraversable()
    {
        var currentNodeIsEmpty = DataManager.Instance.Data.MapData.CurrentNode == null || DataManager.Instance.Data.MapData.CurrentNode.type == MapNodeType.Empty;
        if (type == MapNodeType.Start && currentNodeIsEmpty) return true;
        if (DataManager.Instance.Data.MapData.CurrentNode == null) return false;
        if (DataManager.Instance.Data.MapData.CurrentNode.Next.Any(node => node.x == x && node.y == y)) return true;
        return false;
    }

    public IEnumerator TraverseToNode(VirtualMapNode traversalNode)
    {
        // create a black overlay and fade it in
        yield return StartCoroutine(FadeToBlackCoroutine(1f));

        // update current node in data manager
        DataManager.Instance.Data.MapData.CurrentNode = traversalNode;
        DataManager.Instance.SaveData();
        Debug.Log($"type:{type} X:{x} Y:{y} clicked on traversable node");

        // load encounter scene based on node type
        LoadNextSceneForNode(traversalNode);

    }



    private static void LoadNextSceneForNode(VirtualMapNode traversalNode)
    {
        switch (traversalNode.type) {
            case MapNodeType.Treasure:
                // Load treasure scene
                UnityEngine.SceneManagement.SceneManager.LoadScene("TreasureScene");
                break;
            case MapNodeType.PoolEncounter:
                // Load pool encounter scene
                UnityEngine.SceneManagement.SceneManager.LoadScene("UIScene");
                break;
            case MapNodeType.Shop:
                // Load shop scene
                UnityEngine.SceneManagement.SceneManager.LoadScene("ShopScene");
                break;
            case MapNodeType.RandomEvent:
                // Load random event scene
                UnityEngine.SceneManagement.SceneManager.LoadScene("RandomEventScene");
                break;
            case MapNodeType.Start:
                // Starting node, perhaps load a special scene or just return
                Debug.Log("Starting node clicked.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("UIScene");
                break;
            default:
                Debug.LogWarning("Unknown node type.");
                break;
        }
    }

    private IEnumerator FadeToBlackCoroutine(float duration) {
        // Create overlay
        var fadeOverlay = new GameObject("FadeOverlay");
        var sr = fadeOverlay.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/WhiteSquare");
        sr.color = new Color(0, 0, 0, 0);
        sr.sortingLayerName = "UI";   // ensure on top
        sr.sortingOrder = 9999;

        // Place just in front of the camera
        var cam = Camera.main;
        fadeOverlay.transform.position = cam.transform.position + cam.transform.forward * 1f;

        // Scale to cover the screen (orthographic-safe)
        if (cam.orthographic) {
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            // Assumes your WhiteSquare sprite is 1 unit; overscale a bit for safety
            fadeOverlay.transform.localScale = new Vector3(width * 100f, height * 100f, 1f);
        } else {
            // For perspective cameras, just use a big scale
            fadeOverlay.transform.localScale = new Vector3(2000, 2000, 1);
        }

        // Fade across frames
        float t = 0f;
        while (t < duration) {
            t += Time.unscaledDeltaTime; // use unscaled if your game might pause Time.timeScale
            float a = Mathf.Clamp01(t / duration);
            sr.color = new Color(0, 0, 0, a);
            yield return null; // let Unity render
        }

        // Ensure fully black
        sr.color = new Color(0, 0, 0, 1f);

    }

}
