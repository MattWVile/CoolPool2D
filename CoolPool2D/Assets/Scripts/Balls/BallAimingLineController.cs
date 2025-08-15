using UnityEngine;

/// <summary>
/// Draws a deterministic aiming line for a ball using manual math (no Physics2D queries).
/// Predicts wall bounces using the exact same reflection/nudge semantics as PoolWorld.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BallAimingLineController : MonoBehaviour
{
    [Header("Prediction settings")]
    [Tooltip("Max number of cushion bounces to show")]
    [SerializeField] private int maxReflections = 5;
    [Tooltip("Max distance the prediction ray will travel before stopping")]
    [SerializeField] private float maxRayDistance = 60f;
    [Tooltip("Small step offset to place the ghost ball just after impact to avoid immediate re-hit")]
    [SerializeField] private float stepOffset = 0.01f; // kept for tiny forward-step but we will nudge along normal like PoolWorld

    [Header("LineRenderer Settings")]
    [SerializeField] private float lineRendererWidthMultiplier = .2f;
    [SerializeField] private int lineRendererStartRoundness = 5;
    [SerializeField] private string lineRendererColourHex = "#BBBBC5";
    [SerializeField, HideInInspector] private Color lineRendererColour;

    private LineRenderer lineRenderer;
    private float ballRadius;

    void Awake()
    {
        ColorUtility.TryParseHtmlString(lineRendererColourHex, out lineRendererColour);
        lineRenderer = ConfigureLineRenderer(lineRendererColour);
        // Determine radius from sprite bounds (assumes uniform scale)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) ballRadius = sr.bounds.size.x * 0.5f;
        else ballRadius = 0.285f; // fallback
    }

    private LineRenderer ConfigureLineRenderer(Color color)
    {
        var newLineRenderer = gameObject.AddComponent<LineRenderer>();
        newLineRenderer.positionCount = 0;
        newLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        newLineRenderer.startColor = color;
        newLineRenderer.endColor = color;
        newLineRenderer.widthMultiplier = lineRendererWidthMultiplier;
        newLineRenderer.numCapVertices = lineRendererStartRoundness;
        newLineRenderer.alignment = LineAlignment.TransformZ;
        return newLineRenderer;
    }

    private void LineRendererStart(Vector2 startPos)
    {
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
    }

    /// <summary>
    /// Show deterministic wall-only trajectory using the same rules as PoolWorld.
    /// direction does not need to be normalized; method normalizes internally.
    /// </summary>
    public void ShowTrajectory(Vector2 startPos, Vector2 direction, float maxDistance = -1f, int reflections = -1)
    {
        if (lineRenderer == null) return;

        if (maxDistance <= 0f) maxDistance = maxRayDistance;
        if (reflections <= 0) reflections = maxReflections;

        LineRendererStart(startPos);

        Vector2 currentPos = startPos;
        Vector2 currentDir = direction.normalized;

        int pointIndex = 1;

        // Pull table bounds & physics params from PoolWorld
        var world = PoolWorld.Instance;
        if (world == null)
        {
            Debug.LogWarning("PoolWorld.Instance not found — cannot show deterministic trajectory.");
            return;
        }

        float tableMinX = world.minimumTableX;
        float tableMaxX = world.maximumTableX;
        float tableMinY = world.minimumTableY;
        float tableMaxY = world.maximumTableY;
        float wallBounciness = world.wallBounciness;
        float separationNudge = world.separationNudge;

        for (int i = 0; i < reflections; i++)
        {
            // Find distance (along normalized direction) to each possible wall contact (center-of-ball contact)
            float bestDistance = float.PositiveInfinity;
            Vector2 hitNormal = Vector2.zero;

            const float eps = 1e-9f;

            // X axis walls
            if (Mathf.Abs(currentDir.x) > eps)
            {
                if (currentDir.x < 0f)
                {
                    // moving left -> possible hit with left wall at x = tableMinX
                    float distanceToLeft = (tableMinX + ballRadius - currentPos.x) / currentDir.x; // denominator negative -> distance positive
                    if (distanceToLeft >= 0f && distanceToLeft < bestDistance)
                    {
                        bestDistance = distanceToLeft;
                        hitNormal = Vector2.right; // wall normal pointing right
                    }
                }
                else
                {
                    // moving right -> possible hit with right wall x = tableMaxX
                    float distanceToRight = (tableMaxX - ballRadius - currentPos.x) / currentDir.x;
                    if (distanceToRight >= 0f && distanceToRight < bestDistance)
                    {
                        bestDistance = distanceToRight;
                        hitNormal = Vector2.left;
                    }
                }
            }

            // Y axis walls
            if (Mathf.Abs(currentDir.y) > eps)
            {
                if (currentDir.y < 0f)
                {
                    float distanceToBottom = (tableMinY + ballRadius - currentPos.y) / currentDir.y;
                    if (distanceToBottom >= 0f && distanceToBottom < bestDistance)
                    {
                        bestDistance = distanceToBottom;
                        hitNormal = Vector2.up;
                    }
                }
                else
                {
                    float distanceToTop = (tableMaxY - ballRadius - currentPos.y) / currentDir.y;
                    if (distanceToTop >= 0f && distanceToTop < bestDistance)
                    {
                        bestDistance = distanceToTop;
                        hitNormal = Vector2.down;
                    }
                }
            }

            // If no valid hit or hit is beyond max distance -> extend to maxDistance and finish
            if (float.IsInfinity(bestDistance) || bestDistance > maxDistance)
            {
                Vector2 endPoint = currentPos + currentDir * maxDistance;
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(pointIndex++, endPoint);
                break;
            }

            // Compute center position at contact (we already included radius in distance calc)
            Vector2 contactCenter = currentPos + currentDir * bestDistance;

            // Add the point to the line
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(pointIndex++, contactCenter);

            // --- Use same reflection rule as PoolWorld ---
            // Decompose currentDir into normal/tangent components (note: currentDir is normalized)
            float normalComponent = Vector2.Dot(currentDir, hitNormal);       // vn
            Vector2 vN = normalComponent * hitNormal;                         // vN
            Vector2 vT = currentDir - vN;                                     // vT (tangent component)

            // New direction follows: v_new = vT - vN * wallBounciness
            Vector2 newDirUnnormalized = vT - vN * wallBounciness;
            if (newDirUnnormalized.sqrMagnitude <= 1e-12f)
            {
                // If numerical, just reflect perfectly
                newDirUnnormalized = Vector2.Reflect(currentDir, hitNormal);
            }
            currentDir = newDirUnnormalized.normalized;

            // Nudge off the wall along the wall normal (matches PoolWorld's slop / nudge)
            currentPos = contactCenter + hitNormal * separationNudge;

            // Also step slightly along the new direction to avoid immediate re-hit on extremely shallow angles
            currentPos += currentDir * stepOffset;

            // Reduce remaining maxDistance by the distance traveled so far
            maxDistance -= bestDistance;
            if (maxDistance <= 0f) break;
        }
    }

    /// <summary>
    /// Clear the line renderer.
    /// </summary>
    public void HideTrajectory()
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount = 0;
        lineRenderer.SetPositions(new Vector3[0]);
    }
}
