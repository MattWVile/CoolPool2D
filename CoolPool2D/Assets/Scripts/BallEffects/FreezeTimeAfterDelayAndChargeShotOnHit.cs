using System.Collections;
using UnityEngine;

public class FreezeTimeAfterDelayAndChargeShotOnHit : BaseBallKissEffect
{
    // Tunables (can be made public if you want to tweak per-effect in Inspector)
    public float delaySeconds = 0.1f;           // small delay after hit before freezing
    public float freezeHoldSeconds = 3.0f;      // how long time stays frozen (real seconds)
    public float defaultShootSpeed = 50.0f;     // auto-shoot speed if player isn't charging
    public float transitionFraction = 0.15f;    // fraction of hold to use for transitions (clamped)
    public float minTransition = 0.05f;         // min transition time
    public float maxTransition = 0.6f;          // max transition time

    protected override void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        var selfBallData = ballKissedEvent.BallData;
        var otherBallData = ballKissedEvent.CollisionBallData;
        if (otherBallData.BallColour != BallColour.Cue) return;


        // start coroutine for the effect (runs in real time)
        StartCoroutine(FreezeThenShootCoroutine());

        hasEffectTriggeredThisShot = true;
    }

    private IEnumerator FreezeThenShootCoroutine()
    {
        // small delay in real time
        yield return new WaitForSecondsRealtime(delaySeconds);

        if (PoolWorld.Instance == null)
        {
            Debug.LogWarning("PoolWorld.Instance missing — cannot apply freeze effect.");
            yield break;
        }

        // compute transition times (clamped)
        float transition = Mathf.Clamp(freezeHoldSeconds * transitionFraction, minTransition, maxTransition);

        // ease down to zero (transition)
        PoolWorld.Instance.SlowTimeToAFreeze(transition);
        // wait for the transition to finish (use realtime)
        yield return new WaitForSecondsRealtime(transition + 0.01f);

        // enable cue UI/etc
        var gameManager = GameManager.Instance;
        CueMovement cueMovement = null;
        if (gameManager != null && gameManager.cue != null)
        {
            cueMovement = gameManager.cue.GetComponent<CueMovement>();
            if (cueMovement != null)
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
        }

        // WAIT: allow player up to freezeHoldSeconds (real time) to release the shot.
        // If they release (Input.GetKeyUp) we will restore time immediately.
        float startRealtime = Time.realtimeSinceStartup;
        float timeoutRealtime = startRealtime + freezeHoldSeconds;

        while (Time.realtimeSinceStartup < timeoutRealtime)
        {
            yield return null;
        }

        // Immediately begin restoring time (transition)
        PoolWorld.Instance.RestoreTimeToNormal(transition);

        // wait for the transition to finish
        yield return new WaitForSecondsRealtime(transition + 0.01f);

        // (Optional) post-restore housekeeping here (UI, effects, etc)
    }

}
