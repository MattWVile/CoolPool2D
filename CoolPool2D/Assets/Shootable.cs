using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Shootable : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Trajectory Settings")]
    [SerializeField] private int maxReflections = 5;
    [SerializeField] private float maxRayDistance = 30f;
    [SerializeField] private float stepOffset = 0.01f;
    [SerializeField] private float skewStrength = 0.25f;
    [SerializeField] private LayerMask ballCollisionMask;
    [SerializeField] private LayerMask railCollisionMask;

    [Header("LineRenderer Settings")]
    private LineRenderer cueBallLineRenderer;
    public LineRenderer objectBallLineRenderer;
    [SerializeField] private float lineRendererWidthMultiplier = .2f;
    [SerializeField] private int lineRendererStartRoundness = 5;
    [SerializeField] private string cueBallLineRendererColourHex = "#BBBBC5";
    [SerializeField, HideInInspector] private Color cueBallLineRendererColour;

    private float ballRadius;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ColorUtility.TryParseHtmlString(cueBallLineRendererColourHex, out cueBallLineRendererColour);
        cueBallLineRenderer = ConfigureLineRenderer(gameObject, cueBallLineRendererColour);
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
        // Keep cue-ball LR start exactly as you had it
        LineRendererStart(cueBallLineRenderer, startPos);

        Vector2 currentPos = startPos;
        Vector2 currentDir = direction.normalized;

        // Separate indices for each LR to avoid cross-contamination
        int cueBallPointIndex = 1;

        for (int i = 0; i < maxReflections; i++)
        {
            // First, try to hit a ball
            var hit = Physics2D.CircleCast(currentPos, ballRadius, currentDir, maxRayDistance, ballCollisionMask);

            if (hit.collider != null)
            {
                var objectBallGameObject = hit.collider.gameObject;
                Vector2 objectBallCenter = objectBallGameObject.transform.position;

                // Ensure the object LR exists; create if needed
                if (objectBallLineRenderer == null)
                {
                    objectBallLineRenderer = ConfigureLineRenderer(objectBallGameObject, objectBallGameObject.GetComponent<SpriteRenderer>().color);
                }

                // ALWAYS set the LR start to the object's current center (prevents leftover 0,0)
                // This also resets the LR for this hit so subsequent points start at index 1.
                objectBallLineRenderer.positionCount = 1;
                objectBallLineRenderer.SetPosition(0, objectBallCenter);

                // object-specific index starts at 1 (position 0 is the object center)
                int objectBallPointIndex = 1;

                // Reconstruct cue-ball center at the moment of impact
                // (hit.point is on the object surface; hit.normal points OUT from the object center)
                Vector2 cueBallCenterAtHit = hit.point - hit.normal * ballRadius;

                // CORRECT direction: from cue center AT HIT toward the object center
                Vector2 directionOfObjectBall = -(objectBallCenter - cueBallCenterAtHit).normalized;

                // Cast the object-ball path against rails
                // Offset the origin slightly forward so the cast doesn't immediately register odd contact
                Vector2 objectCastOrigin = objectBallCenter + directionOfObjectBall * stepOffset;
                var objectBallHit = Physics2D.CircleCast(objectCastOrigin, ballRadius, directionOfObjectBall, maxRayDistance, railCollisionMask);

                if (objectBallHit.collider != null)
                {
                    // Translate the rail contact point to the moving-ball center position at contact
                    Vector2 objectBallCenterHit = objectBallHit.point + objectBallHit.normal * ballRadius;

                    objectBallLineRenderer.positionCount++;
                    objectBallLineRenderer.SetPosition(objectBallPointIndex++, objectBallCenterHit);
                }
                else
                {
                    // No rail hit — extend the line out to max distance from the object's center
                    Vector2 fallbackEnd = objectBallCenter + directionOfObjectBall * (maxRayDistance - ballRadius);
                    objectBallLineRenderer.positionCount++;
                    objectBallLineRenderer.SetPosition(objectBallPointIndex++, fallbackEnd);
                }

                // --- continue processing cue-ball path after the collision ---
                // Draw cue center at impact (same style you already used)
                Vector2 centerHit = hit.point + hit.normal * ballRadius;
                cueBallLineRenderer.positionCount++;
                cueBallLineRenderer.SetPosition(cueBallPointIndex++, centerHit);

                // Update cue direction (bounce) and step off the surface
                currentDir = GetBounceDirection(currentDir, hit.normal);
                currentPos = centerHit + currentDir * stepOffset;

                // Important: continue loop to allow cue to reflect again or hit another ball
                continue;
            }
            else
            {
                // No ball hit — check rails for the cue ball like before
                hit = Physics2D.CircleCast(currentPos, ballRadius, currentDir, maxRayDistance, railCollisionMask);
                if (hit.collider == null)
                {
                    float travel = maxRayDistance - ballRadius;
                    cueBallLineRenderer.positionCount++;
                    cueBallLineRenderer.SetPosition(cueBallPointIndex, currentPos + currentDir * travel);
                    break;
                }

                Vector2 centerHit = hit.point + hit.normal * ballRadius;
                cueBallLineRenderer.positionCount++;
                cueBallLineRenderer.SetPosition(cueBallPointIndex++, centerHit);

                currentDir = GetBounceDirection(currentDir, hit.normal);
                currentPos = centerHit + currentDir * stepOffset;
            }
        }
    }

    public void HideTrajectory()
    {
        // throw away all points
        cueBallLineRenderer.positionCount = 0;

        // (optional) make sure the underlying array is emptied too
        cueBallLineRenderer.SetPositions(new Vector3[0]);
        if (objectBallLineRenderer != null)
        {
            objectBallLineRenderer.positionCount = 0;
            objectBallLineRenderer.SetPositions(new Vector3[0]);
            objectBallLineRenderer = null;
        }
    }

    private void LineRendererStart(LineRenderer lineRenderer, Vector2 startPos)
    {
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
    }

    private LineRenderer ConfigureLineRenderer(GameObject gameObjectToAddLR, Color lineRendererColour)
    {
        LineRenderer lineRenderer = gameObjectToAddLR.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineRendererColour;
        lineRenderer.endColor = lineRendererColour;
        lineRenderer.widthMultiplier = lineRendererWidthMultiplier;
        lineRenderer.numCapVertices = lineRendererStartRoundness;
        lineRenderer.alignment = LineAlignment.TransformZ;
        return lineRenderer;
    }
    private Vector2 GetBounceDirection(Vector2 currentDirection, Vector2 normal)
    {
        // 1) Perfect mirror reflection
        Vector2 reflected = Vector2.Reflect(currentDirection, normal);

        // 2) Compute the tangent (slide along-wall) direction
        Vector2 tangent = new Vector2(-normal.y, normal.x);

        //This dot product tells you how aligned the bounce direction is with the tangent.

        //If the bounce is grazing the wall(shallow angle), the dot product is close to ±1

        //If it's head-on, the dot product is close to 0

        //So this value tells us how much we should skew the bounce.
        float grazing = Vector2.Dot(reflected, tangent);

        // 4) Build and apply your skew
        Vector2 skew = tangent * (skewStrength * grazing);

        // 5) Return the normalized sum
        return (reflected + skew).normalized;
    }
}