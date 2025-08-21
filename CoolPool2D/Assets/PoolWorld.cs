using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public struct RailSegment
{
    public Vector2 start;
    public Vector2 end;
    public RailLocation rail;
}

public class PoolWorld : MonoBehaviour
{
    // NOTE: We no longer define MIN_DIRECTION_EPSILON / MIN_VELOCITY_THRESHOLD here
    // to avoid duplicating them — use the shared values from SharedDeterministicPhysics.

    public static PoolWorld Instance { get; private set; }

    [System.Serializable]
    public struct PocketStruct { public Vector2 center; public float radius; public PocketController pocketController; }
    public List<PocketStruct> pocketList = new List<PocketStruct>();


    [Header("Table Walls")]
    public Dictionary<RailLocation, RailData> railDictionary = new();

    public struct RailData
    {
        public RailController Controller;
        public List<RailSegment> Segments;
    }

    public Dictionary<RailLocation, List<RailSegment>> railSegmentsDictionary = new();

    private RailLocation lastCollidedRail = RailLocation.NoRail; // last rail hit by a ball

    [Header("Physics")]
    [Tooltip("Coefficient for exponential drag per second (0 = no drag).")]
    public float dragPerSecond = 0.25f;
    [Tooltip("Ball-ball normal bounciness (1 = perfectly elastic).")]
    public float ballBounciness = 1f;
    [Tooltip("Cushion normal bounciness (1 = perfectly elastic).")]
    public float railBounciness = 1f;

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

        // Populate pocketList from PocketController GameObjects
        pocketList.Clear();
        foreach (var pocketController in FindObjectsOfType<PocketController>())
        {
            pocketList.Add(new PocketStruct { center = pocketController.transform.position, radius = pocketController.radius, pocketController = pocketController });
        }

        BuildRailSegmentsFromColliders();


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
    private bool IsBallInPocket(Vector2 pos, float radius, out PocketStruct pocketStruct)
    {
        foreach (var pocket in pocketList)
        {
            float distSqr = (pos - pocket.center).sqrMagnitude;
            if (distSqr <= (pocket.radius + radius) * (pocket.radius + radius))
            {
                pocketStruct = pocket;
                return true;
            }
        }
        pocketStruct = default;
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

            // rail collision candidate
            int railCollisionBallIndex = -1;
            Vector2 railCollisionNormal = Vector2.zero;

            // ball-vs-ball collision candidate
            int ballPairIndexA = -1;
            int ballPairIndexB = -1;
            Vector2 ballPairCollisionNormal = Vector2.zero;

            // 1) find earliest rail (rail) collision across all balls
            for (int ballIndex = 0; ballIndex < registeredBalls.Count; ballIndex++)
            {
                DeterministicBall candidateBall = registeredBalls[ballIndex];
                if (!candidateBall.active || candidateBall.velocity.sqrMagnitude < sleepVelocityThresholdSq) continue;

                // Use shared physics to find the earliest rail hit
                var railCollision = SharedDeterministicPhysics.CalculateTimeToRailCollision((Vector2)candidateBall.transform.position, candidateBall.velocity, candidateBall.ballRadius, railSegmentsDictionary, remainingTime, out float candidateTime, out Vector2 candidateNormal);
                if (railCollision != RailLocation.NoRail)
                {
                    if (candidateTime < earliestCollisionTime)
                    {
                        earliestCollisionTime = candidateTime;
                        railCollisionBallIndex = ballIndex;
                        railCollisionNormal = candidateNormal;
                        ballPairIndexA = ballPairIndexB = -1;
                        lastCollidedRail = railCollision;
                    }
                }
            }

            // 2) find earliest ball-vs-ball collision across all pairs (keeps original quadratic logic)
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
                            railCollisionBallIndex = -1;
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

                if (IsBallInPocket(ball.transform.position, ball.ballRadius, out PocketStruct pocket))
                {
                    if (enableDebugLogs) Debug.Log(
                        $"Ball pocketed at pos {ball.transform.position} into pocket {pocket.pocketController}"
                    );

                    // If your PocketOut needs pocket context:
                    pocket.pocketController.PublishBallPocketedEvent(ball.gameObject);
                }
            }

            // 5) resolve earliest event (if any occurred before the end of the slice)
            if (earliestCollisionTime < remainingTime - SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD)
            {
                if (railCollisionBallIndex >= 0)
                {
                    DeterministicBall impactedBall = registeredBalls[railCollisionBallIndex];
                    if (impactedBall.active)
                    {
                        // Use shared reflection to compute new direction and nudge position
                        SharedDeterministicPhysics.ComputeWallReflection(impactedBall.velocity, railCollisionNormal, railBounciness,
                            (Vector2)impactedBall.transform.position, separationNudge, SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD,
                            out Vector2 reflectedDirection, out Vector2 newPositionAfterNudge);

                        // reflectedDirection is normalized; we need to set velocity magnitude appropriately (preserve speed tangent & bounce normal proportion)
                        // However to keep behavior identical to previous code, compute exact velocity reflection:
                        float normalSpeed = Vector2.Dot(impactedBall.velocity, railCollisionNormal);
                        Vector2 normalVelocityComponent = normalSpeed * railCollisionNormal;
                        Vector2 tangentialVelocityComponent = impactedBall.velocity - normalVelocityComponent;
                        impactedBall.velocity = tangentialVelocityComponent - normalVelocityComponent * railBounciness;

                        // nudge off the rail slightly to avoid immediate re-collision
                        impactedBall.transform.position = (Vector2)impactedBall.transform.position + railCollisionNormal * separationNudge;

                        railDictionary[lastCollidedRail].Controller.PublishBallCollidedWithRailEvent(impactedBall.gameObject);
                        lastCollidedRail = RailLocation.NoRail; // reset after processing

                    }
                }
                else if (ballPairIndexA >= 0 && ballPairIndexB >= 0)
                {
                    DeterministicBall ballA = registeredBalls[ballPairIndexA];
                    DeterministicBall ballB = registeredBalls[ballPairIndexB];
                    if (ballA.active && ballB.active)
                    {
                        // Use shared ResolveEqualMassBallCollision (vector version) and assign velocities
                        SharedDeterministicPhysics.ResolveEqualMassBallCollision(ballA.velocity, ballB.velocity, ballPairCollisionNormal, ballBounciness, out Vector2 vAAfter, out Vector2 vBAfter);
                        ballA.velocity = vAAfter;
                        ballB.velocity = vBAfter;

                        // minimal separation along normal to avoid immediate re-detection
                        ballA.transform.position = (Vector2)ballA.transform.position + ballPairCollisionNormal * separationNudge;
                        ballB.transform.position = (Vector2)ballB.transform.position - ballPairCollisionNormal * separationNudge;
                    }
                }
            }

            // 6) consume the advanced time slice
            remainingTime -= Mathf.Max(earliestCollisionTime, SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // draw pockets
        Gizmos.color = Color.black;
        foreach (var pocket in pocketList)
        {
            Gizmos.DrawWireSphere(pocket.center, pocket.radius);
        }

        // ---------- draw rails ----------
        if (railDictionary != null)
        {
            const float endpointMarkerRadius = 0.02f;
            const float normalLength = 0.15f;

            foreach (var kv in railDictionary)
            {
                var segList = kv.Value.Segments;
                if (segList == null) continue;

                foreach (var seg in segList)
                {
                    Color segColor = Color.white;
                    try
                    {
                        switch (seg.rail)
                        {
                            case RailLocation.TopRight: segColor = Color.cyan; break;
                            case RailLocation.TopLeft: segColor = Color.magenta; break;
                            case RailLocation.MiddleRight: segColor = Color.yellow; break;
                            case RailLocation.MiddleLeft: segColor = Color.grey; break;
                            case RailLocation.BottomRight: segColor = Color.blue; break;
                            case RailLocation.BottomLeft: segColor = Color.red; break;
                            default: segColor = Color.white; break;
                        }
                    }
                    catch { segColor = Color.white; } // in case Rail enum changes

                    Gizmos.color = segColor;
                    Vector3 a3 = new Vector3(seg.start.x, seg.start.y, 0f);
                    Vector3 b3 = new Vector3(seg.end.x, seg.end.y, 0f);
                    Gizmos.DrawLine(a3, b3);

                    Gizmos.DrawWireSphere(a3, endpointMarkerRadius);
                    Gizmos.DrawWireSphere(b3, endpointMarkerRadius);

                    // draw segment normal at midpoint
                    Vector2 segVec = seg.end - seg.start;
                    if (segVec.sqrMagnitude > SharedDeterministicPhysics.MIN_DIRECTION_EPSILON)
                    {
                        Vector2 mid = seg.start + segVec * 0.5f;
                        Vector2 normal = new Vector2(-segVec.y, segVec.x).normalized; // perpendicular
                        Gizmos.color = Color.Lerp(segColor, Color.white, 0.45f);
                        Vector3 mid3 = new Vector3(mid.x, mid.y, 0f);
                        Vector3 normalEnd = mid3 + new Vector3(normal.x, normal.y, 0f) * normalLength;
                        Gizmos.DrawLine(mid3, normalEnd);
                    }
                }
            }
        }

        // ---------- draw predicted collisions for each ball ----------
        float lookaheadSeconds = 1.0f;
        const float impactMarkerRadiusMultiplier = 0.5f;
        foreach (var ball in registeredBalls)
        {
            if (ball == null) continue;

            Vector2 pos = ball.transform.position;
            float radius = ball.ballRadius;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0f), radius);

            // draw velocity vector lightly
            Vector2 vel = ball.velocity;
            if (vel.sqrMagnitude > SharedDeterministicPhysics.MIN_DIRECTION_EPSILON)
            {
                Vector3 pos3 = new Vector3(pos.x, pos.y, 0f);
                Vector3 future3 = pos3 + new Vector3(vel.x, vel.y, 0f) * lookaheadSeconds;
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(pos3, future3);
            }

            // only test active balls with meaningful velocity
            if (!ball.active || ball.velocity.sqrMagnitude <= SharedDeterministicPhysics.MIN_DIRECTION_EPSILON) continue;

            if (SharedDeterministicPhysics.CalculateTimeToRailCollision(pos, ball.velocity, radius, railSegmentsDictionary, lookaheadSeconds, out float t, out Vector2 normal) != RailLocation.NoRail)
            {
                // predicted impact point
                Vector2 impact = pos + ball.velocity * t;
                Vector3 impact3 = new Vector3(impact.x, impact.y, 0f);

                // impact marker
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(impact3, radius * impactMarkerRadiusMultiplier);

                // line from ball to impact
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(new Vector3(pos.x, pos.y, 0f), impact3);

                // collision normal
                Gizmos.color = Color.green;
                Vector3 normalEnd = impact3 + new Vector3(normal.x, normal.y, 0f) * (radius + 0.25f);
                Gizmos.DrawLine(impact3, normalEnd);

                // label with time and normal (editor only)
                string label = $"t={t:F3}s\nn=({normal.x:F2},{normal.y:F2})";
                UnityEditor.Handles.Label(impact3 + Vector3.up * 0.02f, label);
            }
        }
    }
#endif

    // Ball-vs-ball collision detection
    private static bool CalculateTimeToBallCollision(
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
        if (quadraticA <= SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD) return false; // no relative motion

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
        if (normalLength <= SharedDeterministicPhysics.MIN_DIRECTION_EPSILON) return false;

        collisionNormal = normalVector / normalLength; // direction from B -> A
        return true;
    }

    private void BuildRailSegmentsFromColliders()
    {
        railDictionary.Clear();

        // Find all markers in scene; you can also use a parent root or tags if preferred.
        var railControllers = FindObjectsOfType<RailController>();
        foreach (var railController in railControllers)
        {
            var collider = railController.GetComponent<Collider2D>();
            if (collider == null)
            {
                if (enableDebugLogs) Debug.LogWarning($"RailColliderMarker on {railController.gameObject.name} has no Collider2D.");
                continue;
            }
            //if(railList.Contains(railController.railLocation))
            if (!railDictionary.ContainsKey(railController.railLocation))
            {
                railDictionary[railController.railLocation] = new RailData { Controller = railController, Segments = new List<RailSegment>() };

            }

            var list = railDictionary[railController.railLocation].Segments;

            // Helper to add a segment given two local points (in collider/transform local space)
            void AddSegmentFromLocalPoints(Vector2 aLocal, Vector2 bLocal)
            {
                // Transform local collider points into world space
                Vector2 aWorld = railController.transform.TransformPoint(aLocal);
                Vector2 bWorld = railController.transform.TransformPoint(bLocal);
                list.Add(new RailSegment { start = aWorld, end = bWorld, rail = railController.railLocation });
                railSegmentsDictionary[railController.railLocation] = railDictionary[railController.railLocation].Segments;
            }

            // Support PolygonCollider2D (may have multiple paths)
            if (collider is PolygonCollider2D poly)
            {
                int pathCount = poly.pathCount;
                for (int path = 0; path < pathCount; path++)
                {
                    var pts = poly.GetPath(path);
                    for (int i = 0; i < pts.Length - 1; i++)
                        AddSegmentFromLocalPoints(pts[i], pts[i + 1]);
                }
            }
        }
    }

    public DeterministicBall GetNextTarget()
    {
        foreach (var ball in registeredBalls)
        {
            if (ball.active && ball.pocketable)
            {
                return ball;
            }
        }
        throw new System.NullReferenceException("No active pocketable balls found.");
    }
}
