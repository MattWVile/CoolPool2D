using UnityEngine;

/// <summary>
/// Per-ball data for deterministic simulation. Movement is performed by PoolWorld.StepAll.
/// </summary>
public class DeterministicBall : MonoBehaviour
{
    [Header("Ball")]
    public float radius = 0.285f;      // tune to your art scale
    public Vector2 velocity = Vector2.zero;
    public bool pocketable = true;

    [HideInInspector] public bool active = true;

    private void OnEnable()
    {
        if (PoolWorld.Instance != null && !PoolWorld.Instance.balls.Contains(this))
            PoolWorld.Instance.balls.Add(this);
        active = true;
    }

    private void OnDisable()
    {
        if (PoolWorld.Instance != null) PoolWorld.Instance.balls.Remove(this);
        active = false;
    }

    public void PocketOut()
    {
        active = false;
        gameObject.SetActive(false);
    }

    // Fire with angle in radians and speed (units/sec)
    public void Shoot(float angleRad, float speed)
    {
        velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * speed;
        // If PoolWorld uses sleepSpeed > 0 make sure the shot is above that threshold
        if (PoolWorld.Instance != null && velocity.sqrMagnitude <= PoolWorld.Instance.sleepSpeed * PoolWorld.Instance.sleepSpeed)
        {
            // tiny bump to ensure it's active
            velocity += new Vector2(1e-4f, 0f);
        }

        if (PoolWorld.Instance != null && PoolWorld.Instance.debug)
            Debug.Log($"{name} shot: vel={velocity}");
    }
}
