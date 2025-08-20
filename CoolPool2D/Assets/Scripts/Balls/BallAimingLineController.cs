using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BallAimingLineController : MonoBehaviour
{
    private enum HitCategory { None, Wall, Ball }

    [Header("Prediction Settings (per ball)")]
    [SerializeField] private int maxReflections = 5;
    [SerializeField] private float maxRayDistance = 60f;
    [SerializeField] private float stepOffset = 0.01f;

    [Header("LineRenderer Settings")]
    [SerializeField] private float lineRendererWidthMultiplier = .2f;
    [SerializeField] private int lineRendererStartRoundness = 5;
    [SerializeField] private string cueBallLineRendererColourHex = "#BBBBC5";
    [SerializeField, HideInInspector] private Color lineRendererColour;

    [Header("Defaults for temporary object previews (if object has no permanent controller)")]
    [SerializeField] private int defaultTempObjectMaxReflections = 3;
    [SerializeField] private float defaultTempObjectMaxRayDistance = 30f;

    private LineRenderer lineRenderer;
    private float ballRadius;
    private int customReflections = -1;
    private float customMaxDistance = -1f;

    private readonly List<BallAimingLineController> activePreviews = new List<BallAimingLineController>();

    internal bool isTemporaryPreview = false;
    private BallAimingLineController previewOwner = null;

    public bool isCueBall => transform.CompareTag("CueBall");

    void Awake()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (isCueBall)
                ColorUtility.TryParseHtmlString(cueBallLineRendererColourHex, out lineRendererColour);
            else
                lineRendererColour = spriteRenderer.color;

            ballRadius = spriteRenderer.bounds.size.x * 0.5f;
        }
        else
        {
            ColorUtility.TryParseHtmlString(cueBallLineRendererColourHex, out lineRendererColour);
            ballRadius = 0.285f;
        }

        lineRenderer = ConfigureLineRenderer(lineRendererColour);
    }

    private LineRenderer ConfigureLineRenderer(Color color)
    {
        var existing = GetComponent<LineRenderer>();
        if (existing != null)
        {
            existing.positionCount = 0;
            existing.startColor = color;
            existing.endColor = color;
            existing.widthMultiplier = lineRendererWidthMultiplier;
            existing.numCapVertices = lineRendererStartRoundness;
            existing.alignment = LineAlignment.TransformZ;
            return existing;
        }

        var newLR = gameObject.AddComponent<LineRenderer>();
        newLR.positionCount = 0;
        newLR.material = new Material(Shader.Find("Sprites/Default"));
        newLR.startColor = color;
        newLR.endColor = color;
        newLR.widthMultiplier = lineRendererWidthMultiplier;
        newLR.numCapVertices = lineRendererStartRoundness;
        newLR.alignment = LineAlignment.TransformZ;
        return newLR;
    }

    private void ApplyVisualSettingsToRenderer()
    {
        if (lineRenderer == null) return;
        lineRenderer.startColor = lineRendererColour;
        lineRenderer.endColor = lineRendererColour;
        lineRenderer.widthMultiplier = lineRendererWidthMultiplier;
        lineRenderer.numCapVertices = lineRendererStartRoundness;
    }

    private void LineRendererStart(Vector2 startPos)
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, new Vector3(startPos.x, startPos.y, transform.position.z));
    }

    public void SetLineColor(Color color)
    {
        lineRendererColour = color;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    public void SetPredictionSettings(float maxDistance, int reflections)
    {
        customMaxDistance = maxDistance;
        customReflections = reflections;
    }

    public void ShowTrajectory(Vector2 startPos, Vector2 direction, float maxDistance = -1f, int reflections = -1)
    {
        if (lineRenderer == null) return;

        var newPreviews = new List<BallAimingLineController>();
        var consumedBalls = new HashSet<DeterministicBall>();

        HideVisualsForActivePreviews();

        if (maxDistance <= 0f)
            maxDistance = customMaxDistance > 0 ? customMaxDistance : maxRayDistance;
        if (reflections <= 0)
            reflections = customReflections > 0 ? customReflections : maxReflections;

        LineRendererStart(startPos);

        Vector2 currentPosition = startPos;
        Vector2 currentDirection = direction.normalized;
        int pointIndex = 1;

        var world = PoolWorld.Instance;
        if (world == null)
        {
            Debug.LogWarning("PoolWorld.Instance not found — cannot show deterministic trajectory.");
            return;
        }

        float wallBounciness = world.wallBounciness;
        float separationNudge = world.separationNudge;

        DeterministicBall selfDeterministicBall = null;
        foreach (var db in world.registeredBalls)
            if (db != null && db.transform == transform) { selfDeterministicBall = db; break; }

        for (int bounce = 0; bounce < reflections; bounce++)
        {
            float bestDistance = float.PositiveInfinity;
            Vector2 hitNormal = Vector2.zero;
            HitCategory hitCategory = HitCategory.None;
            DeterministicBall hitBall = null;

            // --- find rail hit ---
            if (TryFindRailHit(currentPosition, currentDirection, ballRadius, world, maxDistance, out float railDistance, out Vector2 railNormal))
            {
                bestDistance = railDistance;
                hitNormal = railNormal;
                hitCategory = HitCategory.Wall;
            }

            // --- find ball hit ---
            if (TryFindBallHit(currentPosition, currentDirection, ballRadius, world, selfDeterministicBall, consumedBalls,
                maxDistance, out float ballDistance, out Vector2 ballNormal, out DeterministicBall ballHit))
            {
                if (ballDistance < bestDistance)
                {
                    bestDistance = ballDistance;
                    hitNormal = ballNormal;
                    hitCategory = HitCategory.Ball;
                    hitBall = ballHit;
                }
            }

            if (float.IsInfinity(bestDistance) || bestDistance > maxDistance)
            {
                Vector2 endPoint = currentPosition + currentDirection * maxDistance;
                AppendLinePoint(endPoint, ref pointIndex);
                break;
            }

            Vector2 contactCenter = currentPosition + currentDirection * bestDistance;
            AppendLinePoint(contactCenter, ref pointIndex);

            if (hitCategory == HitCategory.Ball && hitBall != null)
            {
                consumedBalls.Add(hitBall);

                Vector2 velocityA = currentDirection; // direction used as A's velocity
                Vector2 velocityB = (Vector2)hitBall.velocity;
                Vector2 collisionNormal = hitNormal.normalized;

                SharedDeterministicPhysics.ResolveEqualMassBallCollision(velocityA, velocityB, collisionNormal, world.ballBounciness, out Vector2 velAAfter, out Vector2 velBAfter);

                currentPosition = contactCenter + collisionNormal * separationNudge;
                currentDirection = (velAAfter.sqrMagnitude > SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD) ? velAAfter.normalized : Vector2.Reflect(currentDirection, collisionNormal);
                currentPosition += currentDirection * stepOffset;

                if (previewOwner == null)
                {
                    var preview = SpawnOrReuseObjectPreview(hitBall, this);
                    if (preview != null && !newPreviews.Contains(preview)) newPreviews.Add(preview);

                    Vector2 objectStart = contactCenter - collisionNormal * separationNudge;
                    Vector2 objectDir = (velBAfter.sqrMagnitude > SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD) ? velBAfter.normalized : Vector2.zero;
                    if (objectDir != Vector2.zero && preview != null)
                    {
                        preview.previewOwner = preview.previewOwner ?? this;
                        preview.ShowTrajectory(objectStart, objectDir);
                    }
                }

                maxDistance -= bestDistance;
                if (maxDistance <= 0f) break;
                continue;
            }
            else // wall
            {
                SharedDeterministicPhysics.ComputeWallReflection(currentDirection, hitNormal, wallBounciness, contactCenter, separationNudge,
                    stepOffset, out Vector2 newDir, out Vector2 newPos);
                currentDirection = newDir;
                currentPosition = newPos;

                maxDistance -= bestDistance;
                if (maxDistance <= 0f) break;
            }
        }

        // cleanup previews that are no longer needed
        foreach (var prev in activePreviews.ToArray())
        {
            if (prev == null) continue;
            if (!newPreviews.Contains(prev))
            {
                prev.HideVisualOnly();
                if (prev.isTemporaryPreview && prev.previewOwner == this)
                {
#if UNITY_EDITOR
                    DestroyImmediate(prev);
#else
                    Destroy(prev);
#endif
                }
            }
        }

        activePreviews.Clear();
        activePreviews.AddRange(newPreviews);
    }

    public void HideTrajectory()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.SetPositions(new Vector3[0]);
        }

        foreach (var preview in activePreviews)
        {
            if (preview == null) continue;
            preview.HideVisualOnly();
            if (preview.isTemporaryPreview && preview.previewOwner == this)
            {
#if UNITY_EDITOR
                DestroyImmediate(preview);
#else
                Destroy(preview);
#endif
            }
        }
        activePreviews.Clear();
        previewOwner = null;
    }

    private void HideVisualOnly()
    {
        if (lineRenderer != null) lineRenderer.positionCount = 0;
    }

    private void HideVisualsForActivePreviews()
    {
        HideVisualOnly();
        for (int i = 0; i < activePreviews.Count; i++)
        {
            var p = activePreviews[i];
            if (p == null) continue;
            p.HideVisualOnly();
        }
    }

    // New: find earliest rail collision (uses shared SharedDeterministicPhysics)
    private bool TryFindRailHit(Vector2 position, Vector2 direction, float radius, PoolWorld world, float maxDistance, out float distanceToRail, out Vector2 railNormal)
    {
        distanceToRail = float.PositiveInfinity;
        railNormal = Vector2.zero;
        if (world == null || world.railSegments == null || world.railSegments.Count == 0) return false;

        // We sweep the ball center along direction * t and ask deterministic physics to find earliest
        if (SharedDeterministicPhysics.CalculateTimeToRailCollision(position, direction.normalized, radius, world.railSegments, maxDistance, out float t, out Vector2 normal))
        {
            if (t >= 0f && t <= maxDistance)
            {
                distanceToRail = t;
                railNormal = normal;
                return true;
            }
        }
        return false;
    }

    private bool TryFindBallHit(Vector2 position, Vector2 direction, float radius, PoolWorld world, DeterministicBall self, HashSet<DeterministicBall> ignoredBalls, float maxDistance, out float distanceToBall, out Vector2 collisionNormal, out DeterministicBall hitBall)
    {
        distanceToBall = float.PositiveInfinity;
        collisionNormal = Vector2.zero;
        hitBall = null;

        foreach (var other in world.registeredBalls)
        {
            if (other == null || !other.active) continue;
            if (self != null && other == self) continue;
            if (ignoredBalls != null && ignoredBalls.Contains(other)) continue;

            if (previewOwner != null)
            {
                var t = other.transform;
                if (t != null && t.CompareTag("CueBall")) continue;
            }

            if (CalculateDistanceToBallCollision(position, direction, radius, (Vector2)other.transform.position,
                other.ballRadius, maxDistance, out float dist, out Vector2 normalAtContact))
            {
                if (dist >= 0f && dist < distanceToBall)
                {
                    distanceToBall = dist;
                    collisionNormal = normalAtContact;
                    hitBall = other;
                }
            }
        }

        return hitBall != null;
    }

    private BallAimingLineController SpawnOrReuseObjectPreview(DeterministicBall targetBall, BallAimingLineController owner)
    {
        if (targetBall == null) return null;
        var targetTransform = targetBall.transform;
        if (targetTransform == null) return null;

        var permanent = targetTransform.GetComponent<BallAimingLineController>();
        if (permanent != null && !permanent.isTemporaryPreview)
        {
            permanent.previewOwner = permanent.previewOwner ?? owner;
            var sr = targetTransform.GetComponent<SpriteRenderer>();
            if (sr != null) permanent.SetLineColor(sr.color);
            return permanent;
        }

        var existingTemp = activePreviews.Find(p => p != null && p.transform == targetTransform);
        if (existingTemp != null) return existingTemp;

        var newPreview = targetTransform.gameObject.AddComponent<BallAimingLineController>();
        newPreview.isTemporaryPreview = true;
        newPreview.previewOwner = owner;

        newPreview.maxReflections = defaultTempObjectMaxReflections;
        newPreview.maxRayDistance = defaultTempObjectMaxRayDistance;

        newPreview.lineRendererWidthMultiplier = this.lineRendererWidthMultiplier;
        newPreview.lineRendererStartRoundness = this.lineRendererStartRoundness;

        var targetSR = targetTransform.GetComponent<SpriteRenderer>();
        if (targetSR != null) newPreview.SetLineColor(targetSR.color);

        newPreview.ApplyVisualSettingsToRenderer();

        return newPreview;
    }

    private void AppendLinePoint(Vector2 worldPoint, ref int pointIndex)
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(pointIndex++, new Vector3(worldPoint.x, worldPoint.y, transform.position.z));
    }

    private static bool CalculateDistanceToBallCollision(Vector2 ballPosition, Vector2 directionNormalized, float ballRadius, Vector2 otherPosition, float otherRadius, float maxDistance, out float distance, out Vector2 collisionNormal)
    {
        distance = maxDistance;
        collisionNormal = Vector2.zero;

        Vector2 relativePosition = ballPosition - otherPosition;
        Vector2 rayDirection = directionNormalized;
        float combinedRadius = ballRadius + otherRadius;

        float quadA = Vector2.Dot(rayDirection, rayDirection);
        float quadB = 2f * Vector2.Dot(relativePosition, rayDirection);
        float quadC = Vector2.Dot(relativePosition, relativePosition) - combinedRadius * combinedRadius;

        float discriminant = quadB * quadB - 4f * quadA * quadC;
        if (discriminant < 0f) return false;

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float solution0 = (-quadB - sqrtDiscriminant) / (2f * quadA);
        float solution1 = (-quadB + sqrtDiscriminant) / (2f * quadA);

        float earliestSolution = float.PositiveInfinity;
        if (solution0 >= 0f && solution0 <= maxDistance) earliestSolution = solution0;
        else if (solution1 >= 0f && solution1 <= maxDistance) earliestSolution = solution1;
        else return false;

        Vector2 positionAtCollision = ballPosition + rayDirection * earliestSolution;
        Vector2 collisionVector = positionAtCollision - otherPosition;
        float collisionLength = collisionVector.magnitude;
        if (collisionLength <= SharedDeterministicPhysics.MIN_DIRECTION_EPSILON) return false;

        collisionNormal = collisionVector / collisionLength;
        distance = earliestSolution;
        return true;
    }
}
