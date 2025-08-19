using System.Collections.Generic;
using UnityEngine;

public struct RailSegment
{
    public Vector2 start;
    public Vector2 end;
    public Rail rail;
}

public class PoolWorld : MonoBehaviour
{

    private const float MIN_DIRECTION_EPSILON = 1e-9f;
    private const float MIN_VELOCITY_THRESHOLD = 1e-12f;
    public static PoolWorld Instance { get; private set; }

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

    [Header("Table Walls")]
    public Dictionary<Rail, List<RailSegment>> railSegments = new();




    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("Multiple PoolWorld instances found. Using the last one.");
        Instance = this;

        // Populate pocketList from PocketController GameObjects
        pocketList.Clear();
        foreach (var pocketCtrl in FindObjectsOfType<PocketController>())
        {
            pocketList.Add(new Pocket { center = pocketCtrl.transform.position, radius = pocketCtrl.radius });
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

                if (CalculateTimeToRailCollision((Vector2)candidateBall.transform.position, candidateBall.velocity,candidateBall.ballRadius, railSegments, remainingTime, out float candidateTime, out Vector2 candidateNormal))
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

                        //if (enableDebugLogs) Debug.Log($"Wall collision: ball {wallCollisionBallIndex} at t={earliestCollisionTime}, normal {wallCollisionNormal}, new vel {impactedBall.velocity}");
                    }
                }
                else if (ballPairIndexA >= 0 && ballPairIndexB >= 0)
                {
                    DeterministicBall ballA = registeredBalls[ballPairIndexA];
                    DeterministicBall ballB = registeredBalls[ballPairIndexB];
                    if (ballA.active && ballB.active)
                    {
                        //if (enableDebugLogs) Debug.Log($"Ball collision: pair ({ballPairIndexA},{ballPairIndexB}) at t={earliestCollisionTime} normal {ballPairCollisionNormal}");

                        ResolveEqualMassBallCollision(ballA, ballB, ballPairCollisionNormal, ballBounciness);

                        // minimal separation along normal to avoid immediate re-detection
                        ballA.transform.position = (Vector2)ballA.transform.position + ballPairCollisionNormal * separationNudge;
                        ballB.transform.position = (Vector2)ballB.transform.position - ballPairCollisionNormal * separationNudge;

                        //if (enableDebugLogs) Debug.Log($"After resolve: vA={ballA.velocity}, vB={ballB.velocity}");
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
        // draw pockets (existing)
        Gizmos.color = Color.black;
        foreach (var pocket in pocketList)
        {
            Gizmos.DrawWireSphere(pocket.center, pocket.radius);
        }

        // ---------- draw rails ----------
        if (railSegments != null)
        {
            const float endpointMarkerRadius = 0.02f;
            const float normalLength = 0.15f;

            foreach (var kv in railSegments)
            {
                var segList = kv.Value;
                if (segList == null) continue;

                foreach (var seg in segList)
                {
                    // choose color by rail (customize as you like)
                    Color segColor = Color.white;
                    try
                    {
                        switch (seg.rail)
                        {
                            case Rail.TopRight: segColor = Color.cyan; break;
                            case Rail.TopLeft: segColor = Color.magenta; break;
                            case Rail.MiddleRight: segColor = Color.yellow; break;
                            case Rail.MiddleLeft: segColor = Color.grey; break;
                            case Rail.BottomRight: segColor = Color.blue; break;
                            case Rail.BottomLeft: segColor = Color.red; break;
                            default: segColor = Color.white; break;
                        }
                    }
                    catch { segColor = Color.white; } // in case Rail enum changes

                    Gizmos.color = segColor;
                    Vector3 a3 = new Vector3(seg.start.x, seg.start.y, 0f);
                    Vector3 b3 = new Vector3(seg.end.x, seg.end.y, 0f);
                    Gizmos.DrawLine(a3, b3);

                    // endpoint markers
                    Gizmos.DrawWireSphere(a3, endpointMarkerRadius);
                    Gizmos.DrawWireSphere(b3, endpointMarkerRadius);

                    // draw segment normal at midpoint
                    Vector2 segVec = seg.end - seg.start;
                    if (segVec.sqrMagnitude > MIN_DIRECTION_EPSILON)
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
        // How far ahead to look for a collision when visualizing (seconds). Tweak as needed.
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
            if (vel.sqrMagnitude > MIN_DIRECTION_EPSILON)
            {
                Vector3 pos3 = new Vector3(pos.x, pos.y, 0f);
                Vector3 future3 = pos3 + new Vector3(vel.x, vel.y, 0f) * lookaheadSeconds;
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(pos3, future3);
            }

            // only test active balls with meaningful velocity
            if (!ball.active || ball.velocity.sqrMagnitude <= MIN_DIRECTION_EPSILON) continue;

            if (CalculateTimeToRailCollision(pos, ball.velocity, radius, railSegments, lookaheadSeconds, out float t, out Vector2 normal))
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

    // ------------------ Replace old AABB-based method with this ------------------
    private bool CalculateTimeToRailCollision(Vector2 ballPosition, Vector2 ballVelocity, float ballRadius, Dictionary<Rail, List<RailSegment>> rails, float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {
        timeToCollision = maxSimulationTime;
        collisionNormal = Vector2.zero;
        bool hit = false;

        if (rails == null || rails.Count == 0) return false;

        // iterate every rail segment
        foreach (var kv in rails)
        {
            var segList = kv.Value;
            if (segList == null) continue;
            foreach (var seg in segList)
            {
                if (CalculateTimeToSegmentCollision(
                    ballPosition, ballVelocity, ballRadius,
                    seg.start, seg.end,
                    maxSimulationTime, out float candidateTime, out Vector2 candidateNormal))
                {
                    if (candidateTime < timeToCollision)
                    {
                        timeToCollision = candidateTime;
                        collisionNormal = candidateNormal;
                        hit = true;
                    }
                }
            }
        }

        return hit;
    }

    // Helper: test swept collision between moving point (ball center) and a segment treated as a capsule (line core + endpoint caps).
    private static bool CalculateTimeToSegmentCollision(Vector2 ballPosition, Vector2 ballVelocity, float ballRadius, Vector2 segA, Vector2 segB, float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
    {
        timeToCollision = maxSimulationTime;
        collisionNormal = Vector2.zero;
        bool found = false;

        Vector2 seg = segB - segA;
        float segLenSq = seg.sqrMagnitude;
        if (segLenSq <= MIN_DIRECTION_EPSILON) return false; // degenerate segment

        Vector2 segUnit = seg / Mathf.Sqrt(segLenSq);
        Vector2 segNormal = new Vector2(-segUnit.y, segUnit.x); // perpendicular (arbitrary orientation)

        // --- 1) collision with the infinite line containing the segment (then check projection) ---
        float distToLine = Vector2.Dot(ballPosition - segA, segNormal); // signed distance along segNormal
        float relVelAlongNormal = Vector2.Dot(ballVelocity, segNormal);

        if (Mathf.Abs(relVelAlongNormal) > MIN_VELOCITY_THRESHOLD)
        {
            // times when center-line distance equals ±ballRadius
            // t = (±ballRadius - distToLine) / relVelAlongNormal
            float t1 = (ballRadius - distToLine) / relVelAlongNormal;
            float t2 = (-ballRadius - distToLine) / relVelAlongNormal;

            // we want the earliest t >= 0
            float tLine = float.PositiveInfinity;
            if (t1 >= 0f && t1 <= maxSimulationTime) tLine = Mathf.Min(tLine, t1);
            if (t2 >= 0f && t2 <= maxSimulationTime) tLine = Mathf.Min(tLine, t2);

            if (!float.IsPositiveInfinity(tLine))
            {
                // check that the contact point projects onto the segment
                Vector2 posAtT = ballPosition + ballVelocity * tLine;
                // point on line at minimal distance from posAtT (without offset)
                float projParam = Vector2.Dot(posAtT - segA, seg) / segLenSq; // 0..1 relative parameter
                if (projParam >= 0f && projParam <= 1f)
                {
                    // compute the exact closest point on segment and normal
                    Vector2 closestPoint = segA + projParam * seg;
                    Vector2 normalVec = posAtT - closestPoint;
                    float nlen = normalVec.magnitude;
                    if (nlen > MIN_DIRECTION_EPSILON)
                    {
                        Vector2 normalDir = normalVec / nlen;
                        // normalDir points from segment -> ball; that's what we want
                        if (tLine < timeToCollision)
                        {
                            timeToCollision = tLine;
                            collisionNormal = normalDir;
                            found = true;
                        }
                    }
                }
            }
        }

        // --- 2) collisions with segment endpoints (caps) ---
        // Solve |(ballPosition + ballVelocity * t) - endpoint|^2 = ballRadius^2
        // Quadratic in t: (v·v) t^2 + 2(s·v) t + (s·s - r^2) = 0  where s = ballPosition - endpoint
        float vDotV = Vector2.Dot(ballVelocity, ballVelocity);

        // endpoint A
        {
            Vector2 s = ballPosition - segA;
            float c = Vector2.Dot(s, s) - ballRadius * ballRadius;
            if (vDotV > MIN_VELOCITY_THRESHOLD) // moving relative to endpoint
            {
                float b = 2f * Vector2.Dot(s, ballVelocity);
                float disc = b * b - 4f * vDotV * c;
                if (disc >= 0f)
                {
                    float sqrtD = Mathf.Sqrt(disc);
                    float r0 = (-b - sqrtD) / (2f * vDotV);
                    float r1 = (-b + sqrtD) / (2f * vDotV);
                    float tCandidate = float.PositiveInfinity;
                    if (r0 >= 0f && r0 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r0);
                    if (r1 >= 0f && r1 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r1);
                    if (!float.IsPositiveInfinity(tCandidate) && tCandidate < timeToCollision)
                    {
                        Vector2 posAtT = ballPosition + ballVelocity * tCandidate;
                        Vector2 normalVec = posAtT - segA;
                        float nlen = normalVec.magnitude;
                        if (nlen > MIN_DIRECTION_EPSILON)
                        {
                            collisionNormal = normalVec / nlen;
                            timeToCollision = tCandidate;
                            found = true;
                        }
                    }
                }
            }
            else
            {
                // not moving (or extremely slow) -> only collide if already overlapping (shouldn't happen in normal sim)
                if (Mathf.Abs(c) <= 0f)
                {
                    // ignore: near-instant overlap handled elsewhere
                }
            }
        }

        // endpoint B
        {
            Vector2 s = ballPosition - segB;
            float c = Vector2.Dot(s, s) - ballRadius * ballRadius;
            if (vDotV > MIN_VELOCITY_THRESHOLD)
            {
                float b = 2f * Vector2.Dot(s, ballVelocity);
                float disc = b * b - 4f * vDotV * c;
                if (disc >= 0f)
                {
                    float sqrtD = Mathf.Sqrt(disc);
                    float r0 = (-b - sqrtD) / (2f * vDotV);
                    float r1 = (-b + sqrtD) / (2f * vDotV);
                    float tCandidate = float.PositiveInfinity;
                    if (r0 >= 0f && r0 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r0);
                    if (r1 >= 0f && r1 <= maxSimulationTime) tCandidate = Mathf.Min(tCandidate, r1);
                    if (!float.IsPositiveInfinity(tCandidate) && tCandidate < timeToCollision)
                    {
                        Vector2 posAtT = ballPosition + ballVelocity * tCandidate;
                        Vector2 normalVec = posAtT - segB;
                        float nlen = normalVec.magnitude;
                        if (nlen > MIN_DIRECTION_EPSILON)
                        {
                            collisionNormal = normalVec / nlen;
                            timeToCollision = tCandidate;
                            found = true;
                        }
                    }
                }
            }
        }

        return found && timeToCollision <= maxSimulationTime;
    }

    /// <summary>
    /// Calculates earliest time (<= maxSimulationTime) when two moving circles will touch.
    /// Uses quadratic formula on relative motion. Returns true and normal (B -> A) if collision occurs.
    /// </summary>
    private static bool CalculateTimeToBallCollision(Vector2 ballAPosition, Vector2 ballAVelocity, float ballARadius, Vector2 ballBPosition, Vector2 ballBVelocity, float ballBRadius, float maxSimulationTime, out float timeToCollision, out Vector2 collisionNormal)
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

    private void BuildRailSegmentsFromColliders()
    {
        railSegments.Clear();

        // Find all markers in scene; you can also use a parent root or tags if preferred.
        var markers = FindObjectsOfType<RailColliderMarker>();
        foreach (var marker in markers)
        {
            var col = marker.GetComponent<Collider2D>();
            if (col == null)
            {
                if (enableDebugLogs) Debug.LogWarning($"RailColliderMarker on {marker.gameObject.name} has no Collider2D.");
                continue;
            }

            if (!railSegments.ContainsKey(marker.rail))
                railSegments[marker.rail] = new List<RailSegment>();

            var list = railSegments[marker.rail];

            // Helper to add a segment given two local points (in collider/transform local space)
            void AddSegmentFromLocalPoints(Vector2 aLocal, Vector2 bLocal)
            {
                // Transform local collider points into world space
                Vector2 aWorld = marker.transform.TransformPoint(aLocal);
                Vector2 bWorld = marker.transform.TransformPoint(bLocal);
                list.Add(new RailSegment { start = aWorld, end = bWorld, rail = marker.rail });
            }

            // Support PolygonCollider2D (may have multiple paths)
            if (col is PolygonCollider2D poly)
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
}
