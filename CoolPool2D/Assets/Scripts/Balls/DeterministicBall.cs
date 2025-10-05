using UnityEngine;

public class DeterministicBall : MonoBehaviour
{
    [Header("Ball")]
    public float ballRadius;
    public Vector2 velocity = Vector2.zero;
    public bool pocketable = true;
    public bool isShootable = false;


    public Vector2 initialVelocity = Vector2.zero;
    public Vector2 stationaryPosition = Vector2.zero;

    [HideInInspector] public bool active = true;

    public void Start()
    {
        stationaryPosition = (Vector2)transform.position;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BallStoppedEvent>(OnBallStopped);
        ballRadius = GetComponent<SpriteRenderer>().bounds.size.x / 2f;
        if (PoolWorld.Instance != null && !PoolWorld.Instance.registeredBalls.Contains(this))
        {
            PoolWorld.Instance.registeredBalls.Add(this);
        }
        active = true;
    }

    private void OnDisable()
    {

        EventBus.Unsubscribe<BallStoppedEvent>(OnBallStopped);
        if (PoolWorld.Instance != null) PoolWorld.Instance.registeredBalls.Remove(this);
        active = false;
    }
    // Fire with angle in radians and speed (units/sec)
    private void OnBallStopped(BallStoppedEvent ballStoppedEvent)
    {
        stationaryPosition = (Vector2)transform.position;
        // TODO move this to new TurnManager
        gameObject.GetComponent<BallData>().numberOfOnBallHitEffectsTriggeredThisTurn = 0;
    }

    public void Shoot(float angleRad, float speed, GameObject target,  bool isInitialShot = true)
    {
        if (isInitialShot)
        { 
            GameManager.Instance.CaptureCurrentShotSnapshot();
            EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target });
        }
        else
        {
            GameStateManager.Instance.SubmitEndOfState(GameState.Shooting);
        }
            velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * speed;
        if (PoolWorld.Instance != null && velocity.sqrMagnitude <= PoolWorld.Instance.sleepVelocityThreshold * PoolWorld.Instance.sleepVelocityThreshold)
        {
            // tiny bump to ensure it's active
            velocity += new Vector2(1e-4f, 0f);
        }
        initialVelocity = velocity;
    }

}
