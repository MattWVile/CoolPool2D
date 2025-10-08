using UnityEngine;

public class MapNodeUI : MonoBehaviour
{
    private MapNode mapNode;

    [SerializeField] private float speed = 3f;      // How fast it pulsates
    [SerializeField] private float amount = 0.1f;   // How much it scales up/down
    private Vector3 baseScale;


    public bool isTraversable
    {
        get
        {
            if (mapNode == null) return false;
            return mapNode.IsTraversable();
        }
    }

    void Start()
    {
        mapNode = GetComponent<MapNode>();
        baseScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (isTraversable) Pulsate();
    }

    private void Pulsate()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * amount;
        transform.localScale = baseScale * scale;
    }
}
