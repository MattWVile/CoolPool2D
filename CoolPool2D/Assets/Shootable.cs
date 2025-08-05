using UnityEngine;

public class Shootable : MonoBehaviour
{
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    public int trajectoryPoints = 30;
    public float timeStep = 0.1f;
    public LayerMask collisionMask;
    public float ballRadius;// Adjust this based on your ball size

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        Color lineRendererColor;
        ColorUtility.TryParseHtmlString("#BBBBC5", out lineRendererColor);
        lineRenderer.startColor = lineRendererColor;
        lineRenderer.endColor = lineRendererColor;
        ballRadius = GetComponent<SpriteRenderer>().bounds.size.x / 2f;
    }


    public void Shoot(float aimingAngle, float power)
    {
        var force = new Vector2(Mathf.Cos(aimingAngle), Mathf.Sin(aimingAngle)) * power;
        rb.AddForce(force);
        HideTrajectory();
    }

    public void ShowTrajectory(Vector2 startPos, Vector2 direction, float power)
    {
        int reflections = 5;
        float maxDistance = 30f;
        LineRendererSetup(startPos);

        Vector2 currentPos = startPos;
        Vector2 currentDir = direction.normalized;

        int pointIndex = 1;

        for (int i = 0; i < reflections; i++)
        {
            // Subtract ball radius from maxDistance to account for ball edge
            RaycastHit2D hit = Physics2D.Raycast(currentPos, currentDir, maxDistance - ballRadius, collisionMask);

            if (hit.collider != null)
            {
                // Move hit point outwards along hit normal by ballRadius to represent ball edge collision
                Vector2 adjustedHitPoint = hit.point + hit.normal * ballRadius;

                lineRenderer.positionCount++;
                lineRenderer.SetPosition(pointIndex, adjustedHitPoint);
                pointIndex++;

                // Reflect direction off surface
                currentDir = Vector2.Reflect(currentDir, hit.normal);

                // Start next raycast slightly beyond adjusted hit point along reflected direction
                currentPos = adjustedHitPoint + currentDir * 0.01f;
            }
            else
            {
                // No hit? Draw line maxDistance ahead minus ball radius
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(pointIndex, currentPos + currentDir * (maxDistance));
                break;
            }
        }
    }

    private void LineRendererSetup(Vector2 startPos)
    {
        // This still uses world units but avoids tapering or fading
        lineRenderer.widthMultiplier = .2f;
        lineRenderer.numCapVertices = 5; // Makes ends look rounder
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPos);
    }

    public void HideTrajectory()
    {
        lineRenderer.positionCount = 0;
    }
}