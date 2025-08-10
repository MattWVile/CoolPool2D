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
        LineRendererStart(cueBallLineRenderer, startPos);

        Vector2 currentPos = startPos;
        Vector2 currentDir = direction.normalized;
        int pointIndex = 1;

        for (int i = 0; i < maxReflections; i++)
        {
            var hit = Physics2D.CircleCast(currentPos, ballRadius, currentDir, maxRayDistance, ballCollisionMask);
            if (hit.collider != null)
            {
                var objectBallGameObject = hit.collider.gameObject;
                if (objectBallLineRenderer == null)
                {
                    objectBallLineRenderer = ConfigureLineRenderer(objectBallGameObject, objectBallGameObject.GetComponent<SpriteRenderer>().color);
                    LineRendererStart(objectBallLineRenderer, currentPos);
                }
                //calculate current direction using the angle of the hit.
                Vector2 center = objectBallGameObject.transform.position;
                Vector2 collisionPoint = hit.point;

                //var objectBallHit = Physics2D.CircleCast(objectBallGameObject.transform.position, ballRadius, directionOfObjectBall, maxRayDistance, railCollisionMask);

            }
            else
            {
                hit = Physics2D.CircleCast(currentPos, ballRadius, currentDir, maxRayDistance, railCollisionMask);
                if (hit.collider == null)
                {
                    float travel = maxRayDistance - ballRadius;
                    cueBallLineRenderer.positionCount++;
                    cueBallLineRenderer.SetPosition(pointIndex, currentPos + currentDir * travel);
                    break;
                }
            }

            Vector2 centerHit = hit.point + hit.normal * ballRadius;
            cueBallLineRenderer.positionCount++;
            cueBallLineRenderer.SetPosition(pointIndex++, centerHit);

            currentDir = GetBounceDirection(currentDir, hit.normal);
            currentPos = centerHit + currentDir * stepOffset;

        }
    }
    public void HideTrajectory()
    {
        // throw away all points
        cueBallLineRenderer.positionCount = 0;

        // (optional) make sure the underlying array is emptied too
        cueBallLineRenderer.SetPositions(new Vector3[0]);
        objectBallLineRenderer = null;
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
    private Vector2 GetBounceDirection(Vector2 inDir, Vector2 normal)
    {
        // 1) Perfect mirror reflection
        Vector2 reflected = Vector2.Reflect(inDir, normal);

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
