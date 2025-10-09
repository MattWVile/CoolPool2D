using System.Collections;
using UnityEngine;

public class FreezeTimeAfterDelayAndShootAgainOnHit : MonoBehaviour
{
    public float delaySeconds = 0.1f;           // small delay after hit before freezing
    public float freezeDuration = 3.0f;      // how long time stays frozen (real seconds)
    public float defaultShootSpeed = 50.0f;     // auto-shoot speed if player isn't charging
    public float freezeTransitionDuration = 0.3f;    // fraction of hold to use for transitions (clamped)
    public float minTransition = 0.05f;         // min transition time
    public float maxTransition = 0.8f;          // max transition time


    public GameManager gameManager;
    public CueMovement cueMovement;
    public void Start()
    {
        gameManager = GameManager.Instance;
        cueMovement = gameManager.cue.GetComponent<CueMovement>();
        EventBus.Subscribe<BallKissedEvent>(OnBallKissedEvent);
        gameObject.GetComponent<BallData>().numberOfOnBallHitEffects++;
    }
    void OnDestroy()
    {
        EventBus.Unsubscribe<BallKissedEvent>(OnBallKissedEvent);
    }

    public void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        BallData otherBallData = ballKissedEvent.BallData;
        BallData selfBallData = ballKissedEvent.CollisionBallData;

        if (otherBallData.BallColour != BallColour.Cue) return;
            
        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn >= selfBallData.numberOfOnBallHitEffects) return;

        
        PoolWorld.Instance.RunFreezeCoroutine(FreezeThenShootCoroutine(otherBallData.gameObject));

        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn++;
    }

    private IEnumerator FreezeThenShootCoroutine(GameObject cueBall)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);

        // safety: bail if cue ball got potted during the delay
        var cueBallData = cueBall.GetComponent<BallData>();
        if (cueBallData == null || !cueBall.activeInHierarchy)
            yield break;

        float transition = Mathf.Clamp(freezeDuration * freezeTransitionDuration, minTransition, maxTransition);

        // Ease down to freeze
        PoolWorld.Instance.SlowTimeToAFreeze(transition);
        yield return new WaitForSecondsRealtime(transition + 0.01f);

        // safety: bail if cue ball got potted during transition
        if (cueBallData == null || !cueBall.activeInHierarchy)
        {
            PoolWorld.Instance.RestoreTimeToNormal(transition); // make sure time isn’t stuck
            yield break;
        }

        // Enable cue
        if (gameManager != null && gameManager.cue != null && cueMovement != null)
        {
            try
            {
                var target = PoolWorld.Instance.GetNextTarget();
                cueMovement.Enable(target.gameObject);
            }
            catch
            {
                cueMovement.Enable(null);
            }
        }

        float startRealtime = Time.realtimeSinceStartup;
        float timeoutRealtime = startRealtime + freezeDuration;
        bool shotTaken = false;

        while (Time.realtimeSinceStartup < timeoutRealtime && !shotTaken)
        {
            // bail if cue ball is potted mid-freeze
            if (cueBallData == null || !cueBall.activeInHierarchy)
            {
                PoolWorld.Instance.RestoreTimeToNormal(transition);
                yield break;
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                shotTaken = true;
            }

            yield return null;
        }

        // Auto fire only if still valid
        if (!shotTaken && cueBallData != null && cueBall.activeInHierarchy)
        {
            cueBall.GetComponent<DeterministicBall>()
                   .Shoot(cueMovement.aimingAngle, cueMovement.shotStrength);
        }

        // Disable cue
        cueMovement.RunDisableRoutine(cueMovement.Disable(0.01f));

        // Restore time
        PoolWorld.Instance.RestoreTimeToNormal(transition);
        yield return new WaitForSecondsRealtime(transition + 0.01f);
    }

}
