using UnityEngine;

public class DeterministicBall : MonoBehaviour
{
    [Header("Ball")]
    public float ballRadius;
    public Vector2 velocity = Vector2.zero;
    public bool pocketable = true;
    public bool isShootable = false;

    [HideInInspector] public bool active = true;

    private void OnEnable()
    {

        ballRadius = GetComponent<SpriteRenderer>().bounds.size.x / 2f;
        if (PoolWorld.Instance != null && !PoolWorld.Instance.registeredBalls.Contains(this))
            PoolWorld.Instance.registeredBalls.Add(this);
        active = true;

    }

    private void OnDisable()
    {
        if (PoolWorld.Instance != null) PoolWorld.Instance.registeredBalls.Remove(this);
        active = false;
    }
    // Fire with angle in radians and speed (units/sec)
    public void Shoot(float angleRad, float speed)
    {

        velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * speed;
        if (PoolWorld.Instance != null && velocity.sqrMagnitude <= PoolWorld.Instance.sleepVelocityThreshold * PoolWorld.Instance.sleepVelocityThreshold)
        {
            // tiny bump to ensure it's active
            velocity += new Vector2(1e-4f, 0f);
        }

    }

}
