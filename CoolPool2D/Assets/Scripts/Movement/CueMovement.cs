using System.Collections;
using UnityEngine;

public class CueMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float distanceFromTarget = 4f; // Distance of the cue from the cue ball
    public GameObject target; // The target ball
    private DeterministicBall targetBall; // The deterministic ball script
    public float AimingAngle;

    public float shotStrength = 1f;

    // store unscaled charge start time (null => not charging)
    private float? isChargingStart = null;

    // use unscaled time for charge when frozen
    private float chargeTime => isChargingStart != null
        ? Mathf.Clamp(GetUnscaledTime() - isChargingStart.Value, 0f, 1f)
        : 0f;

    public float aimingSpeed = 1f; // Speed of aiming adjustment

    private void Update()
    {
        SetPosition();
        HandleInput();

        // Trajectory preview only while not charging and when in aiming state
        if (target == null) return;

        BallAimingLineController lineController = target.GetComponent<BallAimingLineController>();
        if (lineController != null && isChargingStart == null && GameStateManager.Instance.CurrentGameState == GameState.Aiming)
        {
            var aimingAngleVector = new Vector2(Mathf.Cos(AimingAngle), Mathf.Sin(AimingAngle));
            lineController.ShowTrajectory(target.transform.position, aimingAngleVector);
        }
    }

    //private void HandleInput()
    //{
    //    // Choose deltaTime depending on whether we're effectively frozen
    //    float dt = (Time.timeScale > 0.001f) ? Time.deltaTime : Time.unscaledDeltaTime;

    //    // Adjust aim speed (these key presses are frame independent)
    //    if (Input.GetKeyDown(KeyCode.W))
    //    {
    //        aimingSpeed = Mathf.Min(aimingSpeed + 0.3f, 1f);
    //    }
    //    if (Input.GetKeyDown(KeyCode.S))
    //    {
    //        aimingSpeed = Mathf.Max(aimingSpeed - 0.3f, 0.1f);
    //    }

    //    // Rotate aim using dt (unscaled while frozen)
    //    float cueMovement = Input.GetAxis("Horizontal");
    //    AimingAngle += cueMovement * dt * aimingSpeed;

    //    // Charging shot — use unscaled time to allow charging while frozen
    //    if (Input.GetKey(KeyCode.Space))
    //    {
    //        if (isChargingStart != null) return;
    //        // start using unscaled time so charges progress while timescale==0
    //        isChargingStart = GetUnscaledTime();
    //    }
    //    else
    //    {
    //        if (isChargingStart == null) return;

    //        // Shoot the ball when release space
    //        if (targetBall != null)
    //        {
    //            // Use chargeTime (0..1) to determine shot strength if desired
    //            float finalStrength = Mathf.Lerp(0.2f, shotStrength, chargeTime);
    //            targetBall.Shoot(AimingAngle, finalStrength);
    //        }

    //        // publish shot event (existing code)
    //        EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target });

    //        isChargingStart = null;
    //    }
    //}

    public IEnumerator Disable(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        target = null;
        targetBall = null;
        spriteRenderer.enabled = false;
    }
    public void Enable(GameObject targetObj)
    {
        target = targetObj;
        // don't cache initialTargetPosition; follow the live target
        targetBall = target.GetComponent<DeterministicBall>();
        spriteRenderer.enabled = true;

        // Initialize aiming angle to point from cue -> target so rotation starts sensibly
        Vector2 dir = (target.transform.position - transform.position);
        if (dir.sqrMagnitude > 1e-6f)
            AimingAngle = Mathf.Atan2(dir.y, dir.x);

        // if the player is already holding space, start charging using unscaled time
        if (Input.GetKey(KeyCode.Space) && isChargingStart == null)
            isChargingStart = GetUnscaledTime();
    }

    private void HandleInput()
    {
        // Choose dt depending on whether we're effectively frozen
        float dt = (Time.timeScale > 0.001f) ? Time.deltaTime : Time.unscaledDeltaTime;

        // Prefer raw axis (no smoothing) and fallback to arrow keys for reliability
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (Mathf.Approximately(horizontal, 0f))
        {
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
            else if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
        }

        // Optional debug:
        // Debug.Log($"Horizontal input: {horizontal}, dt: {dt}");

        // Rotate aim using dt (unscaled while frozen)
        AimingAngle += horizontal * dt * aimingSpeed;

        // Charging shot — use unscaled time to allow charging while frozen
        if (Input.GetKey(KeyCode.Space))
        {
            if (isChargingStart != null) return;
            isChargingStart = GetUnscaledTime();
        }
        else
        {
            if (isChargingStart == null) return;

            // Shoot the ball when release space
            if (targetBall != null)
            {
                float finalStrength = Mathf.Lerp(0.2f, shotStrength, chargeTime);
                targetBall.Shoot(AimingAngle, finalStrength);
            }

            EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target });
            isChargingStart = null;
        }
    }

    private void SetPosition()
    {
        if (target == null) return;

        Vector2 targetWorld = target.transform.position;
        var offset = getOffset(distanceFromTarget - (chargeTime * 3f), AimingAngle);
        Vector2 cuePos = targetWorld + offset;
        transform.position = cuePos;

        // Rotate the cue to face the aim direction
        transform.rotation = Quaternion.Euler(0, 0, AimingAngle * Mathf.Rad2Deg);
    }
    public Vector2 getOffset(float distance, float angle)
    {
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }

    // helper that returns unscaled time (split out so it's clear and easy to change)
    private static float GetUnscaledTime() => Time.unscaledTime;
}
