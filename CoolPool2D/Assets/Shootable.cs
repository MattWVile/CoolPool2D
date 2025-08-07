using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Shootable : MonoBehaviour
{
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;

    [Header("Trajectory Settings")]
    [SerializeField] private int maxReflections = 5;
    [SerializeField] private float maxRayDistance = 30f;
    [SerializeField] private float stepOffset = 0.01f;
    [SerializeField] private float skewStrength = 0.25f;
    [SerializeField] private LayerMask collisionMask;

    [Header("LineRenderer Settings")]
    [SerializeField] private float lineRendererWidthMultiplier = .2f;
    [SerializeField] private int lineRendererStartRoundness = 5;
    [SerializeField] private string lineRendererColourHex = "#BBBBC5";
    [SerializeField, HideInInspector] private Color lineRendererColour;

    private float ballRadius;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ConfigureLineRenderer();
        ballRadius = GetComponent<SpriteRenderer>().bounds.size.x / 2f;
    }

    public void Shoot(float aimingAngle, float power)
    {
        Vector2 force = new Vector2(Mathf.Cos(aimingAngle), Mathf.Sin(aimingAngle)) * power;
        rb.AddForce(force);
        HideTrajectory();
    }

    public void ShowTrajectory(Vector2 startPos, Vector2 direction, float power)
    {
        SetupLineRenderer(startPos);

        Vector2 currentPos = startPos;
        Vector2 currentDir = direction.normalized;
        int pointIndex = 1;

        for (int i = 0; i < maxReflections; i++)
        {
            var hit = Physics2D.CircleCast(currentPos, ballRadius, currentDir, maxRayDistance, collisionMask);
            if (hit.collider == null)
            {
                float travel = maxRayDistance - ballRadius;
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(pointIndex, currentPos + currentDir * travel);
                break;
            }

            Vector2 centerHit = hit.point + hit.normal * ballRadius;

            lineRenderer.positionCount++;
            lineRenderer.SetPosition(pointIndex++, centerHit);

            currentDir = GetBounceDirection(currentDir, hit.normal);
            currentPos = centerHit + currentDir * stepOffset;
        }
    }
    public void HideTrajectory()
    {
        // throw away all points
        lineRenderer.positionCount = 0;

        // (optional) make sure the underlying array is emptied too
        lineRenderer.SetPositions(new Vector3[0]);
    }

    private void SetupLineRenderer(Vector2 startPos)
    {
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
    }

    private void ConfigureLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        ColorUtility.TryParseHtmlString(lineRendererColourHex, out lineRendererColour);
        lineRenderer.startColor = lineRendererColour;
        lineRenderer.endColor = lineRendererColour;
        lineRenderer.widthMultiplier = lineRendererWidthMultiplier;
        lineRenderer.numCapVertices = lineRendererStartRoundness;
        lineRenderer.alignment = LineAlignment.TransformZ;
    }
    private Vector2 GetBounceDirection(Vector2 inDir, Vector2 normal)
    {
        // 1) Perfect mirror reflection
        Vector2 reflected = Vector2.Reflect(inDir, normal);

        // 2) Compute the tangent (along-wall) direction
        Vector2 tangent = new Vector2(-normal.y, normal.x);

        // 3) How “grazing” is the hit?  ±1 for shallow, 0 for head-on
        float grazing = Vector2.Dot(reflected, tangent);

        // 4) Build and apply your skew
        Vector2 skew = tangent * (skewStrength * grazing);

        // 5) Return the normalized sum
        return (reflected + skew).normalized;
    }


}
