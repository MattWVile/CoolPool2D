using System.Collections;
using UnityEngine;

public class FreezeTimeAfterDelayAndShootAgainOnHit : MonoBehaviour, IOnBallHitEffect
{
    // Tunables (can be made public if you want to tweak per-effect in Inspector)
    public float delaySeconds = 0.1f;           // small delay after hit before freezing
    public float freezeHoldSeconds = 3.0f;      // how long time stays frozen (real seconds)
    public float defaultShootSpeed = 50.0f;     // auto-shoot speed if player isn't charging
    public float transitionFraction = 0.15f;    // fraction of hold to use for transitions (clamped)
    public float minTransition = 0.05f;         // min transition time
    public float maxTransition = 0.6f;          // max transition time

    public void OnBallHit(GameObject self, GameObject other)
    {
        // only apply when OTHER is the cue ball
        var otherBallData = other.GetComponent<BallData>();
        if (otherBallData.BallColour != BallColour.White) return;

        // enforce per-turn trigger limits if you're using that counter
        var selfBallData = self.GetComponent<BallData>();
        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn >= selfBallData.numberOfOnBallHitEffects) return;


        // start coroutine for the effect (runs in real time)
        StartCoroutine(FreezeThenShootCoroutine(self, other));
        // mark triggered
        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn++;
    }
    private IEnumerator FreezeThenShootCoroutine(GameObject self, GameObject cueBall)
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
        bool playerReleased = false;

        while (Time.realtimeSinceStartup < timeoutRealtime)
        {
            // If player released the charge this frame, break and restore time.
            //if (Input.GetKeyUp(KeyCode.Space))
            //{
            //    playerReleased = true;
            //    break;
            //}

            // Small yield to next frame (coroutine resumes after Update, so HandleInput has already processed release)
            yield return null;
        }

        // If player did not release while frozen, perform fallback auto-shoot (only if cue ball still nearly stationary)
        //if (!playerReleased)
        //{
        //    var detCueFallback = cueBall.GetComponent<DeterministicBall>();
        //    if (detCueFallback != null)
        //    {
        //        const float stillThreshold = 0.2f;
        //        if (detCueFallback.velocity.magnitude <= stillThreshold)
        //        {
        //            Vector2 dir = detCueFallback.initialVelocity;
        //            if (dir.sqrMagnitude <= 1e-6f) dir = Vector2.right;
        //            dir = dir.normalized;
        //            detCueFallback.velocity = dir * defaultShootSpeed;
        //        }
        //    }
        //}

        // Immediately begin restoring time (transition)
        PoolWorld.Instance.RestoreTimeToNormal(transition);

        // wait for the transition to finish
        yield return new WaitForSecondsRealtime(transition + 0.01f);

        // (Optional) post-restore housekeeping here (UI, effects, etc)
    }

}
