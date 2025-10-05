using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapNode : MonoBehaviour
{
    public string type { get; set; } = string.Empty; // e.g. "normal", "shop", "event", "boss", "treasure"
    public List<MapNode> Prev = new();
    public List<MapNode> Next = new();
    public int x { get; set; }
    public int y { get; set; }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Instantiate(MapNode node)
    {
        type = node.type;
        x = node.x;
        y = node.y;
        Prev = node.Prev;
        Next = node.Next;

        transform.position = new Vector3(x , y , 0);
        DrawPathsToNextNodes();
    }

    public void DrawPathsToNextNodes()
    {
        foreach (var nextNode in Next)
        {
            Debug.DrawLine(new Vector3(x, y, 0), new Vector3(nextNode.x, nextNode.y, 0), Color.red, 100f);
        }
    }

}
