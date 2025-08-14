using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic pool world: single authority that advances all balls and resolves collisions.
/// Attach to one GameObject in scene (PoolTable).
/// </summary>
public class PoolWorld : MonoBehaviour
{
    public static PoolWorld Instance { get; private set; }

    [Header("Table Bounds (Axis-Aligned, world units)")]
    public float minX = -4.68f, maxX = 6.67f;
    public float minY = -2.88f, maxY = 2.92f;

    [System.Serializable]
    public struct Pocket { public Vector2 center; public float radius; }
    [Header("Pockets (optional)")]
    public List<Pocket> pockets = new List<Pocket>();

    [Header("Physics")]
    [Tooltip("Coefficient for exponential drag per second (0 = no drag).")]
    public float dragPerSecond = 0.25f; // small default so you see friction; set 0 to disable
    [Tooltip("Ball-ball normal bounciness (1 = perfectly elastic).")]
    public float ballBounciness = 1f;
    [Tooltip("Cushion normal bounciness (1 = perfectly elastic).")]
    public float wallBounciness = 1f;

    [Header("Solver Controls")]
    [Tooltip("Max collision events processed per frame.")]
    public int maxEventsPerFrame = 256;
    [Tooltip("Small separation to prevent immediate re-collision due to float ties.")]
    public float slopEpsilon = 1e-5f;
    [Tooltip("Speeds below this are treated as rest to avoid jitter (set 0 to always simulate).")]
    public float sleepSpeed = 0f; // 0 for testing — set >0 for production

    [Header("Debugging")]
    public bool debug = true; // enable while testing collisions

    // Registered balls
    internal readonly List<DeterministicBall> balls = new List<DeterministicBall>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Debug.LogWarning("Multiple PoolWorld instances found. Using the last one.");
        Instance = this;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || balls.Count == 0) return;
        StepAll(dt);
    }

    public bool IsInPocket(Vector2 pos, float radius, out int index)
    {
        for (int i = 0; i < pockets.Count; i++)
        {
            float rr = pockets[i].radius;
            if ((pos - pockets[i].center).sqrMagnitude <= (rr - radius) * (rr - radius))
            { index = i; return true; }
        }
        index = -1; return false;
    }

    // --- Global event-driven step for all balls ---
    private void StepAll(float dt)
    {
        float remaining = dt;
        int guard = maxEventsPerFrame;

        // squared sleep threshold for faster checks
        float sleepSq = sleepSpeed * sleepSpeed;

        while (remaining > 0f && guard-- > 0)
        {
            // 1) Find earliest collision across ALL balls (vs walls, vs other balls)
            float earliestT = remaining;
            int wallHitBallIdx = -1;
            Vector2 wallNormal = Vector2.zero;

            int pairI = -1, pairJ = -1;
            Vector2 pairNormal = Vector2.zero;

            // Walls
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                if (!b.active || b.velocity.sqrMagnitude < sleepSq) continue;

                float t; Vector2 n;
                if (TimeToAABB((Vector2)b.transform.position, b.velocity, b.radius,
                               minX, maxX, minY, maxY, remaining, out t, out n))
                {
                    if (t < earliestT)
                    {
                        earliestT = t;
                        wallHitBallIdx = i;
                        wallNormal = n;
                        pairI = pairJ = -1;
                    }
                }
            }

            // Ball↔Ball
            for (int i = 0; i < balls.Count; i++)
            {
                var a = balls[i];
                if (!a.active || a.velocity.sqrMagnitude < sleepSq) continue;

                for (int j = i + 1; j < balls.Count; j++)
                {
                    var b = balls[j];
                    if (!b.active || b.velocity.sqrMagnitude < sleepSq) continue;

                    float t; Vector2 n;
                    if (TimeToBall((Vector2)a.transform.position, a.velocity, a.radius,
                                   (Vector2)b.transform.position, b.velocity, b.radius,
                                   remaining, out t, out n))
                    {
                        if (t < earliestT)
                        {
                            earliestT = t;
                            pairI = i; pairJ = j;
                            pairNormal = n;
                            wallHitBallIdx = -1;
                        }
                    }
                }
            }

            // 2) Advance ALL balls by earliestT and apply drag
            float dragFactor = (dragPerSecond > 0f) ? Mathf.Exp(-dragPerSecond * earliestT) : 1f;
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                if (!b.active) continue;
                b.transform.position = (Vector2)b.transform.position + b.velocity * earliestT;
                if (dragFactor != 1f) b.velocity *= dragFactor;
            }

            // Pocket any that fell in during this advance
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                if (!b.active || !b.pocketable) continue;
                if (IsInPocket(b.transform.position, b.radius, out _))
                {
                    if (debug) Debug.Log($"Ball pocketed at pos {b.transform.position}");
                    b.PocketOut();
                }
            }

            // 3) Resolve
            if (earliestT < remaining - 1e-12f)
            {
                if (wallHitBallIdx >= 0)
                {
                    var b = balls[wallHitBallIdx];
                    if (b.active)
                    {
                        float vn = Vector2.Dot(b.velocity, wallNormal);
                        Vector2 vN = vn * wallNormal;
                        Vector2 vT = b.velocity - vN;
                        b.velocity = vT - vN * wallBounciness;

                        b.transform.position = (Vector2)b.transform.position + wallNormal * slopEpsilon;
                        if (debug) Debug.Log($"Wall collision: ball {wallHitBallIdx} at t={earliestT}, normal {wallNormal}, new vel {b.velocity}");
                    }
                }
                else if (pairI >= 0 && pairJ >= 0)
                {
                    var A = balls[pairI];
                    var B = balls[pairJ];
                    if (A.active && B.active)
                    {
                        if (debug) Debug.Log($"Ball collision: pair ({pairI},{pairJ}) at t={earliestT} normal {pairNormal}");
                        ResolveBallBall(A, B, pairNormal, ballBounciness);

                        A.transform.position = (Vector2)A.transform.position + pairNormal * slopEpsilon;
                        B.transform.position = (Vector2)B.transform.position - pairNormal * slopEpsilon;

                        if (debug) Debug.Log($"After resolve: vA={A.velocity}, vB={B.velocity}");
                    }
                }
            }

            // 4) Consume time (safe epsilon if zero)
            remaining -= Mathf.Max(earliestT, 1e-12f);
        }
    }

    // --- Time of impact: circle vs expanded AABB ---
    private static bool TimeToAABB(
        Vector2 p, Vector2 v, float r,
        float minX, float maxX, float minY, float maxY,
        float maxTime, out float t, out Vector2 normal)
    {
        t = maxTime; normal = Vector2.zero;

        if (Mathf.Abs(v.x) > 1e-12f)
        {
            if (v.x < 0f) // left wall
            {
                float tx = (minX + r - p.x) / v.x;
                if (tx >= 0f && tx <= t) { t = tx; normal = Vector2.right; }
            }
            else // right wall
            {
                float tx = (maxX - r - p.x) / v.x;
                if (tx >= 0f && tx <= t) { t = tx; normal = Vector2.left; }
            }
        }

        if (Mathf.Abs(v.y) > 1e-12f)
        {
            if (v.y < 0f) // bottom wall
            {
                float ty = (minY + r - p.y) / v.y;
                if (ty >= 0f && ty <= t) { t = ty; normal = Vector2.up; }
            }
            else // top wall
            {
                float ty = (maxY - r - p.y) / v.y;
                if (ty >= 0f && ty <= t) { t = ty; normal = Vector2.down; }
            }
        }

        return normal != Vector2.zero && t <= maxTime;
    }

    // --- Time of impact: moving circle vs moving circle ---
    private static bool TimeToBall(
        Vector2 p1, Vector2 v1, float r1,
        Vector2 p2, Vector2 v2, float r2,
        float maxTime, out float t, out Vector2 normal)
    {
        t = maxTime; normal = Vector2.zero;

        Vector2 s = p1 - p2;       // relative pos (A - B)
        Vector2 v = v1 - v2;       // relative vel (A wrt B)
        float R = r1 + r2;

        float a = Vector2.Dot(v, v);
        if (a <= 1e-12f) return false; // no relative motion

        float b = 2f * Vector2.Dot(s, v);
        float c = Vector2.Dot(s, s) - R * R;

        float disc = b * b - 4f * a * c;

        // Debug logging to see why collisions don't occur
        if (Instance != null && Instance.debug)
        {
            Debug.Log($"TimeToBall check: s={s} v={v} R={R} a={a} b={b} c={c} disc={disc}");
        }

        if (disc < 0f) return false;

        float sqrtD = Mathf.Sqrt(disc);
        float t0 = (-b - sqrtD) / (2f * a);
        float t1 = (-b + sqrtD) / (2f * a);

        // Debug roots
        if (Instance != null && Instance.debug)
        {
            Debug.Log($"roots: t0={t0}, t1={t1}, maxTime={maxTime}");
        }

        // Use earliest non-negative root within window
        float cand = float.PositiveInfinity;
        if (t0 >= 0f && t0 <= maxTime) cand = t0;
        else if (t1 >= 0f && t1 <= maxTime) cand = t1;
        else return false;

        t = cand;

        // Normal from B -> A at impact
        Vector2 c1 = p1 + v1 * t;
        Vector2 c2 = p2 + v2 * t;
        Vector2 n = c1 - c2;
        float len = n.magnitude;
        if (len <= 1e-9f) return false;
        normal = n / len;

        if (Instance != null && Instance.debug)
        {
            Debug.Log($"TimeToBall returning t={t}, normal={normal}");
        }

        return true;
    }

    // --- Equal-mass elastic collision with bounciness on normal component ---
    private static void ResolveBallBall(DeterministicBall A, DeterministicBall B, Vector2 normal, float bounciness)
    {
        Vector2 n = normal;
        Vector2 t = new Vector2(-n.y, n.x);

        float A_n = Vector2.Dot(A.velocity, n);
        float B_n = Vector2.Dot(B.velocity, n);
        float A_t = Vector2.Dot(A.velocity, t);
        float B_t = Vector2.Dot(B.velocity, t);

        // Equal-mass elastic swap of normal components, with bounciness
        float A_n_after = B_n * bounciness;
        float B_n_after = A_n * bounciness;

        A.velocity = n * A_n_after + t * A_t;
        B.velocity = n * B_n_after + t * B_t;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Table AABB
        Gizmos.color = Color.green;
        Vector3 a = new Vector3(minX, minY, 0), b = new Vector3(maxX, minY, 0);
        Vector3 c = new Vector3(maxX, maxY, 0), d = new Vector3(minX, maxY, 0);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);

        // Pockets
        Gizmos.color = Color.black;
        foreach (var p in pockets)
            Gizmos.DrawWireSphere(p.center, p.radius);
    }
#endif
}
