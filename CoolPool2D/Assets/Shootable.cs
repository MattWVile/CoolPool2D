using UnityEngine;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        ColorUtility.TryParseHtmlString(lineRendererColourHex, out lineRendererColour);
        lineRenderer.startColor = lineRendererColour;
        lineRenderer.endColor = lineRendererColour;

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
            Vector2 reflected = Vector2.Reflect(currentDir, hit.normal);

            Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);


            float grazingFactor = Vector2.Dot(reflected, tangent);

            Vector2 skew = tangent * skewStrength * grazingFactor;

            currentDir = (reflected + skew).normalized;
            currentPos = centerHit + currentDir * stepOffset;
        }
    }

    private void SetupLineRenderer(Vector2 startPos)
    {
        lineRenderer.widthMultiplier = lineRendererWidthMultiplier;
        lineRenderer.numCapVertices = lineRendererStartRoundness;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
    }

    public void HideTrajectory()
    {
        lineRenderer.positionCount = 0;
    }
}
