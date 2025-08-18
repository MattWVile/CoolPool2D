using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic pool world: single authority that advances all balls and resolves collisions.
/// Attach to one GameObject in scene (PoolTable).
/// </summary>
public class PoolWorld : MonoBehaviour
{

    private const float MIN_DIRECTION_EPSILON = 1e-9f;
    private const float MIN_VELOCITY_THRESHOLD = 1e-12f;
    public static PoolWorld Instance { get; private set; }

    [Header("Table Bounds (Axis-Aligned, world units)")]
    public float minimumTableX = -4.68f;
    public float maximumTableX = 6.67f;
    public float minimumTableY = -2.88f;
    public float maximumTableY = 2.92f;

    [System.Serializable]
    public struct Pocket { public Vector2 center; public float radius; }

    [Header("Pockets (optional)")]
    public List<Pocket> pocketList = new List<Pocket>();

    [Header("Physics")]
    [Tooltip("Coefficient for exponential drag per second (0 = no drag).")]
    public float dragPerSecond = 0.25f;
    [Tooltip("Ball-ball normal bounciness (1 = perfectly elastic).")]
    public float ballBounciness = 1f;
    [Tooltip("Cushion normal bounciness (1 = perfectly elastic).")]
    public float wallBounciness = 1f;

    [Header("Solver Controls")]
    [Tooltip("Max collision events processed per frame.")]
    public int maxCollisionEventsPerFrame = 256;
    [Tooltip("Small separation to prevent immediate re-collision due to float ties.")]
    public float separationNudge = 1e-5f;
    [Tooltip("Speeds below this are treated as rest to avoid jitter (set 0 to always simulate).")]
    public float sleepVelocityThreshold = 0f;

    [Header("Debugging")]
    public bool enableDebugLogs = true;

    /// <summary>List of all deterministic balls registered with the world.</summary>
    internal readonly List<DeterministicBall> registeredBalls = new List<DeterministicBall>();




    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("Multiple PoolWorld instances found. Using the last one.");
        Instance = this;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || registeredBalls.Count == 0) return;
        StepSimulationForAllBalls(deltaTime);
    }

    /// <summary>
    /// Returns true and the pocket index if the given ball center is inside any pocket's effective area.
    /// </summary>
    public bool IsBallInPocket(Vector2 ballCenter, float ballRadius, out int pocketIndex)
    {
        for (int pocketIdx = 0; pocketIdx < pocketList.Count; pocketIdx++)
        {
            float pocketRadius = pocketList[pocketIdx].radius;
            // check if center is within (pocketRadius - ballRadius)
            if ((ballCenter - pocketList[pocketIdx].center).sqrMagnitude <= (pocketRadius - ballRadius) * (pocketRadius - ballRadius))
            {
                pocketIndex = pocketIdx;
                return true;
            }
        }
        pocketIndex = -1;
        return false;
    }

    /// <summary>
    /// Main deterministic simulation driver. Advances time and resolves the earliest events iteratively.
    /// </summary>
    private void StepSimulationForAllBalls(float deltaTime)
    {
        float remainingTime = deltaTime;
        int eventsGuard = maxCollisionEventsPerFrame;
        float sleepVelocityThresholdSq = sleepVelocityThreshold * sleepVelocityThreshold;

        while (remainingTime > 0f && eventsGuard-- > 0)
        {
            // earliest collision within remainingTime (default: end of slice)
            float earliestCollisionTime = remainingTime;

            // wall collision candidate
            int wallCollisionBallIndex = -1;
            Vector2 wallCollisionNormal = Vector2.zero;

            // ball-vs-ball collision candidate
            int ballPairIndexA = -1;
            int ballPairIndexB = -1;
            Vector2 ballPairCollisionNormal = Vector2.zero;

            // 1) find earliest wall collision across all balls
            for (int ballIndex = 0; ballIndex < registeredBalls.Count; ballIndex++)
            {
                DeterministicBall candidateBall = registeredBalls[ballIndex];
                if (!candidateBall.active || candidateBall.velocity.sqrMagnitude < sleepVelocityThresholdSq) continue;

                if (CalculateTimeToAABBCollision((Vector2)candidateBall.transform.position, candidateBall.velocity, candidateBall.ballRadius,
                                                 minimumTableX, maximumTableX, minimumTableY, maximumTableY,
                                                 remainingTime, out float candidateTime, out Vector2 candidateNormal))
                {
                    if (candidateTime < earliestCollisionTime)
                    {
                        earliestCollisionTime = candidateTime;
                        wallCollisionBallIndex = ballIndex;
                        wallCollisionNormal = candidateNormal;
                        ballPairIndexA = ballPairIndexB = -1;
                    }
                }
            }

            // 2) find earliest ball-vs-ball collision across all pairs
            for (int indexA = 0; indexA < registeredBalls.Count; indexA++)
            {
                DeterministicBall ballA = registeredBalls[indexA];
                if (!ballA.active || ballA.velocity.sqrMagnitude < sleepVelocityThresholdSq) continue;

                for (int indexB = indexA + 1; indexB < registeredBalls.Count; indexB++)
                {
                    DeterministicBall ballB = registeredBalls[indexB];
                    if (!ballB.active || ballB.velocity.sqrMagnitude < sleepVelocityThresholdSq) continue;

                    if (CalculateTimeToBallCollision((Vector2)ballA.transform.position, ballA.velocity, ballA.ballRadius,
                                                     (Vector2)ballB.transform.position, ballB.velocity, ballB.ballRadius,
                                                     remainingTime, out float candidateTime, out Vector2 candidateNormal))
                    {
                        if (candidateTime < earliestCollisionTime)
                        {
                            earliestCollisionTime = candidateTime;
                            ballPairIndexA = indexA;
                            ballPairIndexB = indexB;
                            ballPairCollisionNormal = candidateNormal;
                            wallCollisionBallIndex = -1;
                        }
                    }
                }
            }

            // 3) advance all balls up to earliestCollisionTime and apply drag
            float dragFactor = (dragPerSecond > 0f) ? Mathf.Exp(-dragPerSecond * earliestCollisionTime) : 1f;
            for (int ballIndex = 0; ballIndex < registeredBalls.Count; ballIndex++)
            {
                DeterministicBall ball = registeredBalls[ballIndex];
                if (!ball.active) continue;
                ball.transform.position = (Vector2)ball.transform.position + ball.velocity * earliestCollisionTime;
                if (dragFactor != 1f) ball.velocity *= dragFactor;
            }

            // 4) pocket detection (during this advanced slice)
            for (int ballIndex = 0; ballIndex < registeredBalls.Count; ballIndex++)
            {
                DeterministicBall ball = registeredBalls[ballIndex];
                if (!ball.active || !ball.pocketable) continue;
                if (IsBallInPocket(ball.transform.position, ball.ballRadius, out _))
                {
                    if (enableDebugLogs) Debug.Log($"Ball pocketed at pos {ball.transform.position}");
                    ball.PocketOut();
                }
            }

            // 5) resolve earliest event (if any occurred before the end of the slice)
            if (earliestCollisionTime < remainingTime - MIN_VELOCITY_THRESHOLD)
            {
                if (wallCollisionBallIndex >= 0)
                {
                    DeterministicBall impactedBall = registeredBalls[wallCollisionBallIndex];
                    if (impactedBall.active)
                    {
                        // reflect normal component and preserve tangential component, apply wall restitution
                        float normalSpeed = Vector2.Dot(impactedBall.velocity, wallCollisionNormal);
                        Vector2 normalVelocityComponent = normalSpeed * wallCollisionNormal;
                        Vector2 tangentialVelocityComponent = impactedBall.velocity - normalVelocityComponent;
                        impactedBall.velocity = tangentialVelocityComponent - normalVelocityComponent * wallBounciness;

                        // nudge off the wall slightly to avoid immediate re-collision
                        impactedBall.transform.position = (Vector2)impactedBall.transform.position + wallCollisionNormal * separationNudge;

                        if (enableDebugLogs) Debug.Log($"Wall collision: ball {wallCollisionBallIndex} at t={earliestCollisionTime}, normal {wallCollisionNormal}, new vel {impactedBall.velocity}");
                    }
                }
                else if (ballPairIndexA >= 0 && ballPairIndexB >= 0)
                {
                    DeterministicBall ballA = registeredBalls[ballPairIndexA];
                    DeterministicBall ballB = registeredBalls[ballPairIndexB];
                    if (ballA.active && ballB.active)
                    {
                        if (enableDebugLogs) Debug.Log($"Ball collision: pair ({ballPairIndexA},{ballPairIndexB}) at t={earliestCollisionTime} normal {ballPairCollisionNormal}");

                        ResolveEqualMassBallCollision(ballA, ballB, ballPairCollisionNormal, ballBounciness);

                        // minimal separation along normal to avoid immediate re-detection
                        ballA.transform.position = (Vector2)ballA.transform.position + ballPairCollisionNormal * separationNudge;
                        ballB.transform.position = (Vector2)ballB.transform.position - ballPairCollisionNormal * separationNudge;

                        if (enableDebugLogs) Debug.Log($"After resolve: vA={ballA.velocity}, vB={ballB.velocity}");
                    }
                }
            }

            // 6) consume the advanced time slice
            remainingTime -= Mathf.Max(earliestCollisionTime, MIN_VELOCITY_THRESHOLD);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // draw table bounds
        Gizmos.color = Color.green;
        Vector3 bottomLeft = new Vector3(minimumTableX, minimumTableY, 0f);
        Vector3 bottomRight = new Vector3(maximumTableX, minimumTableY, 0f);
        Vector3 topRight = new Vector3(maximumTableX, maximumTableY, 0f);
        Vector3 topLeft = new Vector3(minimumTableX, maximumTableY, 0f);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // draw pockets
        Gizmos.color = Color.black;
        foreach (var pocket in pocketList)
        {
            Gizmos.DrawWireSphere(pocket.center, pocket.radius);
        }
    }
#endif

    /// <summary>
    /// Calculates earliest time (<= maxSimulationTime) when a moving circle will contact the axis-aligned
    /// bounding box expanded by the circle radius. Returns true if a collision occurs within maxSimulationTime.
    /// </summary>
    private static bool CalculateTimeToAABBCollision(
        Vector2 ballPosition, Vector2 ballVelocity, float ballRadius,
        float tableMinX, float tableMaxX, float tableMinY, float tableMaxY,
        float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {
        // NOTE: kept name for clarity; method forwards to the internal implementation below.
        return CalculateTimeToAABBCollision_Internal(ballPosition, ballVelocity, ballRadius,
                                                     tableMinX, tableMaxX, tableMinY, tableMaxY,
                                                     maxSimulationTime, out timeToCollision, out collisionNormal);
    }

    private static bool CalculateTimeToAABBCollision_Internal(
        Vector2 ballPosition, Vector2 ballVelocity, float ballRadius,
        float tableMinX, float tableMaxX, float tableMinY, float tableMaxY,
        float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {
        timeToCollision = maxSimulationTime;
        collisionNormal = Vector2.zero;

        if (Mathf.Abs(ballVelocity.x) > MIN_VELOCITY_THRESHOLD)
        {
            if (ballVelocity.x < 0f) // moving left → possible left wall hit
            {
                float timeToLeftWall = (tableMinX + ballRadius - ballPosition.x) / ballVelocity.x;
                if (timeToLeftWall >= 0f && timeToLeftWall <= timeToCollision)
                {
                    timeToCollision = timeToLeftWall;
                    collisionNormal = Vector2.right;
                }
            }
            else // moving right → possible right wall hit
            {
                float timeToRightWall = (tableMaxX - ballRadius - ballPosition.x) / ballVelocity.x;
                if (timeToRightWall >= 0f && timeToRightWall <= timeToCollision)
                {
                    timeToCollision = timeToRightWall;
                    collisionNormal = Vector2.left;
                }
            }
        }

        if (Mathf.Abs(ballVelocity.y) > MIN_VELOCITY_THRESHOLD)
        {
            if (ballVelocity.y < 0f) // moving down → possible bottom wall hit
            {
                float timeToBottomWall = (tableMinY + ballRadius - ballPosition.y) / ballVelocity.y;
                if (timeToBottomWall >= 0f && timeToBottomWall <= timeToCollision)
                {
                    timeToCollision = timeToBottomWall;
                    collisionNormal = Vector2.up;
                }
            }
            else // moving up → possible top wall hit
            {
                float timeToTopWall = (tableMaxY - ballRadius - ballPosition.y) / ballVelocity.y;
                if (timeToTopWall >= 0f && timeToTopWall <= timeToCollision)
                {
                    timeToCollision = timeToTopWall;
                    collisionNormal = Vector2.down;
                }
            }
        }

        return collisionNormal != Vector2.zero && timeToCollision <= maxSimulationTime;
    }

    /// <summary>
    /// Calculates earliest time (<= maxSimulationTime) when two moving circles will touch.
    /// Uses quadratic formula on relative motion. Returns true and normal (B -> A) if collision occurs.
    /// </summary>
    private static bool CalculateTimeToBallCollision(
        Vector2 ballAPosition, Vector2 ballAVelocity, float ballARadius,
        Vector2 ballBPosition, Vector2 ballBVelocity, float ballBRadius,
        float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {
        return CalculateTimeToBallCollision_Internal(ballAPosition, ballAVelocity, ballARadius,
                                                     ballBPosition, ballBVelocity, ballBRadius,
                                                     maxSimulationTime, out timeToCollision, out collisionNormal);
    }

    private static bool CalculateTimeToBallCollision_Internal(
        Vector2 ballAPosition, Vector2 ballAVelocity, float ballARadius,
        Vector2 ballBPosition, Vector2 ballBVelocity, float ballBRadius,
        float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {
        timeToCollision = maxSimulationTime;
        collisionNormal = Vector2.zero;

        Vector2 relativePosition = ballAPosition - ballBPosition;    // s
        Vector2 relativeVelocity = ballAVelocity - ballBVelocity;    // v
        float combinedRadius = ballARadius + ballBRadius;            // R

        float quadraticA = Vector2.Dot(relativeVelocity, relativeVelocity);
        if (quadraticA <= MIN_VELOCITY_THRESHOLD) return false; // no relative motion

        float quadraticB = 2f * Vector2.Dot(relativePosition, relativeVelocity);
        float quadraticC = Vector2.Dot(relativePosition, relativePosition) - combinedRadius * combinedRadius;

        float discriminant = quadraticB * quadraticB - 4f * quadraticA * quadraticC;
        if (discriminant < 0f) return false; // no real roots → no collision

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float root0 = (-quadraticB - sqrtDiscriminant) / (2f * quadraticA);
        float root1 = (-quadraticB + sqrtDiscriminant) / (2f * quadraticA);

        float earliestValidRoot = float.PositiveInfinity;
        if (root0 >= 0f && root0 <= maxSimulationTime) earliestValidRoot = root0;
        else if (root1 >= 0f && root1 <= maxSimulationTime) earliestValidRoot = root1;
        else return false;

        timeToCollision = earliestValidRoot;

        Vector2 positionAtCollisionA = ballAPosition + ballAVelocity * timeToCollision;
        Vector2 positionAtCollisionB = ballBPosition + ballBVelocity * timeToCollision;
        Vector2 normalVector = positionAtCollisionA - positionAtCollisionB;
        float normalLength = normalVector.magnitude;
        if (normalLength <= MIN_DIRECTION_EPSILON) return false;

        collisionNormal = normalVector / normalLength; // direction from B -> A
        return true;
    }

    public static void ResolveEqualMassBallCollision(DeterministicBall ballA, DeterministicBall ballB, Vector2 collisionNormal, float restitution)
    {
        Vector2 normal = collisionNormal.normalized;
        Vector2 tangent = new Vector2(-normal.y, normal.x);

        float velocityNormalA = Vector2.Dot(ballA.velocity, normal);
        float velocityNormalB = Vector2.Dot(ballB.velocity, normal);
        float velocityTangentA = Vector2.Dot(ballA.velocity, tangent);
        float velocityTangentB = Vector2.Dot(ballB.velocity, tangent);

        // swap normal components (equal mass) and apply restitution
        float velocityNormalAAfter = velocityNormalB * restitution;
        float velocityNormalBAfter = velocityNormalA * restitution;

        ballA.velocity = normal * velocityNormalAAfter + tangent * velocityTangentA;
        ballB.velocity = normal * velocityNormalBAfter + tangent * velocityTangentB;
    }

}
