using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RailSegment
{
    public Vector2 start;
    public Vector2 end;
    public RailLocation rail;
}

public struct JawSegment
{
    public Vector2 start;
    public Vector2 end;
    public JawLocation jaw;
}

public class PoolWorld : MonoBehaviour
{
    public static PoolWorld Instance { get; private set; }

    [Serializable]
    public struct PocketStruct { public Vector2 center; public float radius; public PocketController pocketController; }
    public List<PocketStruct> pocketList = new List<PocketStruct>();

    [Header("Legacy Controllers")]
    public Dictionary<RailLocation, RailData> railDictionary = new();
    public Dictionary<JawLocation, JawData> jawDictionary = new();

    public struct RailData
    {
        public RailController Controller;
        public List<RailSegment> Segments;
    }

    public struct JawData
    {
        public JawController Controller;
        public List<JawSegment> Segments;
    }

    public Dictionary<RailLocation, List<RailSegment>> railSegmentsDictionary = new();
    public Dictionary<JawLocation, List<JawSegment>> jawSegmentsDictionary = new();

    public enum EdgeType { Rail = 0, Jaw = 1 }

    public readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public EdgeType Type { get; }
        public int EnumValue { get; }

        public EdgeKey(EdgeType type, int enumValue) { Type = type; EnumValue = enumValue; }
        public static EdgeKey From(RailLocation r) => new EdgeKey(EdgeType.Rail, (int)r);
        public static EdgeKey From(JawLocation j) => new EdgeKey(EdgeType.Jaw, (int)j);

        public bool Equals(EdgeKey other) => other.Type == Type && other.EnumValue == EnumValue;
        public override bool Equals(object obj) => obj is EdgeKey k && Equals(k);
        public override int GetHashCode() => ((int)Type * 397) ^ EnumValue;
        public override string ToString() => Type == EdgeType.Rail ? ((RailLocation)EnumValue).ToString() : ((JawLocation)EnumValue).ToString();
    }

    public struct EdgeSegment
    {
        public Vector2 start;
        public Vector2 end;
        public EdgeKey key;
    }

    public struct EdgeData
    {
        public MonoBehaviour Controller; // RailController or JawController
        public List<EdgeSegment> Segments;
    }

    public Dictionary<EdgeKey, EdgeData> edgeDictionary = new();
    public Dictionary<EdgeKey, List<EdgeSegment>> edgeSegmentsDictionary = new();

    private EdgeKey? lastCollidedEdge = null;

    [Header("Physics")]
    public float dragPerSecond = 0.25f;
    public float ballBounciness = 1f;
    public float railBounciness = 1f;

    [Header("Solver")]
    public int maxCollisionEventsPerFrame = 256;
    public float separationNudge = 1e-5f;
    public float sleepVelocityThreshold = 0f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    internal readonly List<DeterministicBall> registeredBalls = new List<DeterministicBall>();

    private Coroutine _timeScaleRoutine = null;
    private float _originalFixedDeltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) Debug.LogWarning("Multiple PoolWorld instances found. Using the last one.");
        Instance = this;
        _originalFixedDeltaTime = Time.fixedDeltaTime;

        pocketList.Clear();
        foreach (var pocketController in FindObjectsOfType<PocketController>())
            pocketList.Add(new PocketStruct { center = pocketController.transform.position, radius = pocketController.radius, pocketController = pocketController });

        BuildEdgeSegmentsFromColliders();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || registeredBalls.Count == 0) return;
        StepSimulationForAllBalls(dt);
    }

    private bool IsBallInPocket(Vector2 pos, float radius, out PocketStruct pocket)
    {
        foreach (var p in pocketList)
        {
            float distSqr = (pos - p.center).sqrMagnitude;
            float limit = (p.radius + radius);
            if (distSqr <= limit * limit) { pocket = p; return true; }
        }
        pocket = default;
        return false;
    }

    private void StepSimulationForAllBalls(float deltaTime)
    {
        float remaining = deltaTime;
        int guard = maxCollisionEventsPerFrame;
        float sleepVelSq = sleepVelocityThreshold * sleepVelocityThreshold;

        while (remaining > 0f && guard-- > 0)
        {
            float earliest = remaining;
            int railBallIndex = -1;
            Vector2 railNormal = Vector2.zero;

            int pairA = -1, pairB = -1;
            Vector2 pairNormal = Vector2.zero;
            EdgeKey? edgeHitKey = null;

            for (int i = 0; i < registeredBalls.Count; i++)
            {
                var b = registeredBalls[i];
                if (!b.active || b.velocity.sqrMagnitude < sleepVelSq) continue;

                if (CalculateTimeToEdgeCollision((Vector2)b.transform.position, b.velocity, b.ballRadius,
                    edgeSegmentsDictionary, remaining, out EdgeKey foundKey, out float tCandidate, out Vector2 normalCandidate))
                {
                    if (tCandidate < earliest)
                    {
                        earliest = tCandidate;
                        railBallIndex = i;
                        railNormal = normalCandidate;
                        pairA = pairB = -1;
                        edgeHitKey = foundKey;
                    }
                }
            }

            for (int a = 0; a < registeredBalls.Count; a++)
            {
                var A = registeredBalls[a];
                if (!A.active || A.velocity.sqrMagnitude < sleepVelSq) continue;

                for (int b = a + 1; b < registeredBalls.Count; b++)
                {
                    var B = registeredBalls[b];
                    if (!B.active || B.velocity.sqrMagnitude < sleepVelSq) continue;

                    if (CalculateTimeToBallCollision((Vector2)A.transform.position, A.velocity, A.ballRadius,
                        (Vector2)B.transform.position, B.velocity, B.ballRadius,
                        remaining, out float tPair, out Vector2 nPair))
                    {
                        if (tPair < earliest)
                        {
                            earliest = tPair;
                            pairA = a; pairB = b;
                            pairNormal = nPair;
                            railBallIndex = -1;
                            edgeHitKey = null;
                        }
                    }
                }
            }

            MoveBallsAndSimulateDrag(earliest);

            for (int i = 0; i < registeredBalls.Count; i++)
            {
                var b = registeredBalls[i];
                if (!b.active || !b.pocketable) continue;
                if (IsBallInPocket(b.transform.position, b.ballRadius, out PocketStruct pocket))
                {
                    if (enableDebugLogs) Debug.Log($"Ball pocketed at {b.transform.position} into {pocket.pocketController}");
                    pocket.pocketController.PublishBallPocketedEvent(b.gameObject);
                }
            }

            if (earliest < remaining - SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD)
            {
                if (railBallIndex >= 0 && edgeHitKey.HasValue)
                {
                    // TODO fix error when potting balls here
                    var impacted = registeredBalls[railBallIndex];
                    if (impacted.active)
                    {
                        SharedDeterministicPhysics.ComputeWallReflection(impacted.velocity, railNormal, railBounciness,
                            (Vector2)impacted.transform.position, separationNudge, SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD,
                            out Vector2 reflectedDir, out Vector2 newPos);

                        float normalSpeed = Vector2.Dot(impacted.velocity, railNormal);
                        Vector2 normalComp = normalSpeed * railNormal;
                        Vector2 tangentComp = impacted.velocity - normalComp;
                        impacted.velocity = tangentComp - normalComp * railBounciness;

                        impacted.transform.position = (Vector2)impacted.transform.position + railNormal * separationNudge;

                        PublishBallCollidedWithEdgeEvent(edgeHitKey.Value, impacted.gameObject);
                    }
                }
                else if (pairA >= 0 && pairB >= 0)
                {
                    var ballA = registeredBalls[pairA];
                    var ballB = registeredBalls[pairB];
                    if (ballA.active && ballB.active)
                    {
                        SharedDeterministicPhysics.ResolveEqualMassBallCollision(ballA.velocity, ballB.velocity, pairNormal, ballBounciness, out Vector2 aAfter, out Vector2 bAfter);
                        ballA.velocity = aAfter;
                        ballB.velocity = bAfter;
                        PublishBallKissedEvent(ballA, ballB);
                        ballA.initialVelocity = ballA.velocity;
                        ballB.initialVelocity = ballB.velocity;

                        ballA.transform.position = (Vector2)ballA.transform.position + pairNormal * separationNudge;
                        ballB.transform.position = (Vector2)ballB.transform.position - pairNormal * separationNudge;
                    }
                }
            }

            remaining -= Mathf.Max(earliest, SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD);
        }
    }

    private static bool CalculateTimeToBallCollision(
        Vector2 aPos, Vector2 aVel, float aRad,
        Vector2 bPos, Vector2 bVel, float bRad,
        float maxT, out float timeToCollision, out Vector2 collisionNormal)
    {
        timeToCollision = maxT;
        collisionNormal = Vector2.zero;

        Vector2 s = aPos - bPos;
        Vector2 v = aVel - bVel;
        float R = aRad + bRad;

        float A = Vector2.Dot(v, v);
        if (A <= SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD) return false;

        float B = 2f * Vector2.Dot(s, v);
        float C = Vector2.Dot(s, s) - R * R;

        float D = B * B - 4f * A * C;
        if (D < 0f) return false;

        float sqrtD = Mathf.Sqrt(D);
        float r0 = (-B - sqrtD) / (2f * A);
        float r1 = (-B + sqrtD) / (2f * A);

        float root = float.PositiveInfinity;
        if (r0 >= 0f && r0 <= maxT) root = r0;
        else if (r1 >= 0f && r1 <= maxT) root = r1;
        else return false;

        timeToCollision = root;
        Vector2 posA = aPos + aVel * root;
        Vector2 posB = bPos + bVel * root;
        Vector2 n = posA - posB;
        float len = n.magnitude;
        if (len <= SharedDeterministicPhysics.MIN_DIRECTION_EPSILON) return false;
        collisionNormal = n / len;
        return true;
    }

    private bool CalculateTimeToEdgeCollision(Vector2 p0, Vector2 v, float radius, Dictionary<EdgeKey, List<EdgeSegment>> edges, float maxTime, out EdgeKey hitKey, out float time, out Vector2 normal)
    {
        time = maxTime;
        normal = Vector2.zero;
        hitKey = default;
        bool found = false;
        const float eps = 1e-6f;

        foreach (var kv in edges)
        {
            var key = kv.Key;
            var segs = kv.Value;
            if (segs == null) continue;

            foreach (var seg in segs)
            {
                Vector2 A = seg.start;
                Vector2 B = seg.end;
                Vector2 AB = B - A;
                float abLen2 = AB.sqrMagnitude;
                if (abLen2 <= SharedDeterministicPhysics.MIN_DIRECTION_EPSILON)
                {
                    // treat as endpoint-only (degenerate)
                    if (SolvePointCollision(p0, v, radius, A, maxTime, out float tPoint, out Vector2 nPoint))
                    {
                        if (tPoint < time)
                        {
                            time = tPoint;
                            normal = nPoint;
                            hitKey = key;
                            found = true;
                        }
                    }
                    continue;
                }

                Vector2 n = new Vector2(-AB.y, AB.x).normalized;

                float a = Vector2.Dot(p0 - A, n);
                float b = Vector2.Dot(v, n);

                if (Mathf.Abs(b) > eps)
                {
                    float t1 = (radius - a) / b;
                    float t2 = (-radius - a) / b;
                    float[] candidates = { t1, t2 };
                    for (int ci = 0; ci < 2; ci++)
                    {
                        float t = candidates[ci];
                        if (t < 0f || t > maxTime) continue;

                        Vector2 point = p0 + v * t;
                        float s = Vector2.Dot(point - A, AB) / abLen2;
                        if (s >= 0f - 1e-6f && s <= 1f + 1e-6f)
                        {
                            Vector2 hitNormal = Vector2.Dot(v, n) > 0f ? -n : n;
                            if (t < time)
                            {
                                time = t;
                                normal = hitNormal;
                                hitKey = key;
                                found = true;
                            }
                        }
                    }
                }

                if (SolvePointCollision(p0, v, radius, A, maxTime, out float tA, out Vector2 nA))
                {
                    if (tA < time)
                    {
                        time = tA;
                        normal = nA;
                        hitKey = key;
                        found = true;
                    }
                }
                if (SolvePointCollision(p0, v, radius, B, maxTime, out float tB, out Vector2 nB))
                {
                    if (tB < time)
                    {
                        time = tB;
                        normal = nB;
                        hitKey = key;
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    private static bool SolvePointCollision(Vector2 p0, Vector2 v, float r, Vector2 point, float maxT, out float tOut, out Vector2 normalOut)
    {
        tOut = maxT;
        normalOut = Vector2.zero;

        Vector2 m = p0 - point;
        float A = Vector2.Dot(v, v);
        float B = 2f * Vector2.Dot(m, v);
        float C = Vector2.Dot(m, m) - r * r;

        if (A <= SharedDeterministicPhysics.MIN_VELOCITY_THRESHOLD)
            return false;

        float D = B * B - 4f * A * C;
        if (D < 0f) return false;

        float sqrtD = Mathf.Sqrt(D);
        float r0 = (-B - sqrtD) / (2f * A);
        float r1 = (-B + sqrtD) / (2f * A);

        float root = float.PositiveInfinity;
        if (r0 >= 0f && r0 <= maxT) root = r0;
        else if (r1 >= 0f && r1 <= maxT) root = r1;
        else return false;

        Vector2 hitPos = p0 + v * root;
        Vector2 n = hitPos - point;
        float len = n.magnitude;
        if (len <= SharedDeterministicPhysics.MIN_DIRECTION_EPSILON) return false;
        normalOut = n / len;
        tOut = root;
        return true;
    }

    private void BuildEdgeSegmentsFromColliders()
    {
        railDictionary.Clear();
        jawDictionary.Clear();
        railSegmentsDictionary.Clear();
        jawSegmentsDictionary.Clear();

        edgeDictionary.Clear();
        edgeSegmentsDictionary.Clear();

        EdgeData GetOrCreateEdgeData(EdgeKey key, MonoBehaviour controller)
        {
            if (!edgeDictionary.TryGetValue(key, out var data))
            {
                data = new EdgeData { Controller = controller, Segments = new List<EdgeSegment>() };
            }
            else
            {
                if (data.Controller == null) data.Controller = controller;
            }
            edgeDictionary[key] = data;
            edgeSegmentsDictionary[key] = data.Segments;
            return data;
        }

        foreach (var rc in FindObjectsOfType<RailController>())
        {
            var coll = rc.GetComponent<Collider2D>();
            if (coll == null) { if (enableDebugLogs) Debug.LogWarning($"RailController {rc.name} missing Collider2D"); continue; }

            if (!railDictionary.ContainsKey(rc.railLocation))
                railDictionary[rc.railLocation] = new RailData { Controller = rc, Segments = new List<RailSegment>() };

            var segList = railDictionary[rc.railLocation].Segments;

            void AddSegment(Vector2 aLocal, Vector2 bLocal)
            {
                Vector2 aWorld = rc.transform.TransformPoint(aLocal);
                Vector2 bWorld = rc.transform.TransformPoint(bLocal);
                segList.Add(new RailSegment { start = aWorld, end = bWorld, rail = rc.railLocation });
                railSegmentsDictionary[rc.railLocation] = segList;

                var key = EdgeKey.From(rc.railLocation);
                var edge = GetOrCreateEdgeData(key, rc);
                edge.Segments.Add(new EdgeSegment { start = aWorld, end = bWorld, key = key });
                edgeSegmentsDictionary[key] = edge.Segments;
                edgeDictionary[key] = edge;
            }

            if (coll is PolygonCollider2D poly)
            {
                for (int p = 0; p < poly.pathCount; p++)
                {
                    var pts = poly.GetPath(p);
                    for (int i = 0; i < pts.Length - 1; i++) AddSegment(pts[i], pts[i + 1]);
                }
            }
            else if (coll is EdgeCollider2D edge)
            {
                var pts = edge.points;
                for (int i = 0; i < pts.Length - 1; i++) AddSegment(pts[i], pts[i + 1]);
            }
            else if (coll is BoxCollider2D box)
            {
                Vector2 half = box.size * 0.5f;
                Vector2 aLocal = new Vector2(-half.x, -half.y) + box.offset;
                Vector2 bLocal = new Vector2(half.x, -half.y) + box.offset;
                AddSegment(aLocal, bLocal);
            }
        }

        foreach (var jc in FindObjectsOfType<JawController>())
        {
            var coll = jc.GetComponent<Collider2D>();
            if (coll == null) { if (enableDebugLogs) Debug.LogWarning($"JawController {jc.name} missing Collider2D"); continue; }

            if (!jawDictionary.ContainsKey(jc.jawLocation))
                jawDictionary[jc.jawLocation] = new JawData { Controller = jc, Segments = new List<JawSegment>() };

            var segList = jawDictionary[jc.jawLocation].Segments;

            void AddJawSegment(Vector2 aLocal, Vector2 bLocal)
            {
                Vector2 aWorld = jc.transform.TransformPoint(aLocal);
                Vector2 bWorld = jc.transform.TransformPoint(bLocal);
                segList.Add(new JawSegment { start = aWorld, end = bWorld, jaw = jc.jawLocation });
                jawSegmentsDictionary[jc.jawLocation] = segList;

                var key = EdgeKey.From(jc.jawLocation);
                var edge = GetOrCreateEdgeData(key, jc);
                edge.Segments.Add(new EdgeSegment { start = aWorld, end = bWorld, key = key });
                edgeSegmentsDictionary[key] = edge.Segments;
                edgeDictionary[key] = edge;
            }

            if (coll is PolygonCollider2D poly)
            {
                for (int p = 0; p < poly.pathCount; p++)
                {
                    var pts = poly.GetPath(p);
                    for (int i = 0; i < pts.Length - 1; i++) AddJawSegment(pts[i], pts[i + 1]);
                }
            }
            else if (coll is EdgeCollider2D edge)
            {
                var pts = edge.points;
                for (int i = 0; i < pts.Length - 1; i++) AddJawSegment(pts[i], pts[i + 1]);
            }
            else if (coll is BoxCollider2D box)
            {
                Vector2 half = box.size * 0.5f;
                Vector2 aLocal = new Vector2(-half.x, -half.y) + box.offset;
                Vector2 bLocal = new Vector2(half.x, -half.y) + box.offset;
                AddJawSegment(aLocal, bLocal);
            }
        }

        if (enableDebugLogs) Debug.Log($"Built edges: rails={railDictionary.Count}, jaws={jawDictionary.Count}, edges={edgeDictionary.Count}");
    }

    private void MoveBallsAndSimulateDrag(float dt)
    {
        float dragFactor = (dragPerSecond > 0f) ? Mathf.Exp(-dragPerSecond * dt) : 1f;
        for (int i = 0; i < registeredBalls.Count; i++)
        {
            var ball = registeredBalls[i];
            if (!ball.active) continue;
            ball.transform.position = (Vector2)ball.transform.position + ball.velocity * dt;

            if (Mathf.Abs(ball.velocity.x) < .25f && Mathf.Abs(ball.velocity.y) < .25f)
            {
                float strong = Mathf.Exp(-4f * dt);
                ball.velocity *= strong;
                if (ball.velocity.magnitude < 0.005f) ball.velocity = Vector2.zero;
            }
            else if (Mathf.Abs(ball.velocity.x) < 1f && Mathf.Abs(ball.velocity.y) < 1f)
            {
                float strong = Mathf.Exp(-1.05f * dt);
                ball.velocity *= strong;
            }
            else
            {
                if (dragFactor != 1f) ball.velocity *= dragFactor;
            }
        }
    }

    public DeterministicBall GetNextTarget()
    {
        foreach (var b in registeredBalls) if (b.active && b.isShootable) return b;
        throw new NullReferenceException("No active shootable balls found.");
    }

    private void PublishBallKissedEvent(DeterministicBall a, DeterministicBall b)
    {
        var aData = a.gameObject.GetComponent<BallData>();
        var bData = b.gameObject.GetComponent<BallData>();
        string header = $"{aData.BallColour} ball kissed {bData.BallColour} ball";

        var evt = new BallKissedEvent
        {
            Sender = this,
            BallData = aData,
            CollisionBallData = bData,
            ScoreTypeHeader = header,
            ScoreTypePoints = aData.BallPoints + bData.BallPoints,
            IsFoul = false
        };
        EventBus.Publish(evt);

        Debug.Log($"[BallKissedEvent] ball: {aData.BallColour} kissed {bData.BallColour}");
    }

    private void PublishBallCollidedWithEdgeEvent(EdgeKey key, GameObject ball)
    {
        if (!edgeDictionary.TryGetValue(key, out var data)) return;
        if (key.Type == EdgeType.Rail)
        {
            var rc = data.Controller as RailController;
            rc?.PublishBallCollidedWithRailEvent(ball);
        }
        else
        {
            var jc = data.Controller as JawController;
            jc?.PublishBallCollidedWithJawEvent(ball);
        }
    }

    public void SlowTimeToAFreeze(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        if (_timeScaleRoutine != null) StopCoroutine(_timeScaleRoutine);
        _timeScaleRoutine = StartCoroutine(LerpTimeScaleCoroutine(Time.timeScale, 0f, seconds, true));
    }

    public void RestoreTimeToNormal(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        if (_timeScaleRoutine != null) StopCoroutine(_timeScaleRoutine);
        _timeScaleRoutine = StartCoroutine(LerpTimeScaleCoroutine(Time.timeScale, 1f, seconds, false));
    }

    private IEnumerator LerpTimeScaleCoroutine(float from, float to, float duration, bool easeOut)
    {
        if (duration <= Mathf.Epsilon)
        {
            Time.timeScale = to;
            Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;
            _timeScaleRoutine = null;
            yield break;
        }

        float freezePower = 3f;
        float restorePower = 2.2f;
        float power = easeOut ? freezePower : restorePower;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = easeOut ? (1f - Mathf.Pow(1f - t, power)) : Mathf.Pow(t, power);
            float val = Mathf.Lerp(from, to, eased);
            Time.timeScale = Mathf.Clamp01(val);
            Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;
            yield return null;
        }
        Time.timeScale = Mathf.Clamp01(to);
        Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;
        _timeScaleRoutine = null;
    }

    public void RunFreezeCoroutine(IEnumerator routine) => StartCoroutine(routine);
}
