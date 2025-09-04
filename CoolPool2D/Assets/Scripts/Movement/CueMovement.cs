using System.Collections;
using UnityEditor;
using UnityEngine;

public class CueMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float distanceFromTarget = 4f; // Distance of the cue from the cue ball
    public GameObject target; // The target ball
    public DeterministicBall targetBall; // The deterministic ball script
    public float aimingAngle;
    public bool fineAimingActive = false;

    private Vector2 previousMouseLocation;

    public float shotStrength = 1f;

    private float horizontalNudgeAmount = 0f;

    // store unscaled charge start time (null => not charging)
    private float? isChargingStart = null;

    // use unscaled time for charge when frozen
    private float chargeTime => isChargingStart != null
        ? Mathf.Clamp(GetUnscaledTime() - isChargingStart.Value, 0f, 1f)
        : 0f;

    public float aimingSpeed = 1f; // (unused for mouse-based immediate aim but kept for compatibility)

    private void Update()
    {
        SetPosition();
        HandleInput();

        // Trajectory preview only while not charging and when in aiming state
        if (target == null) return;

        BallAimingLineController lineController = target.GetComponent<BallAimingLineController>();
        if (lineController != null && isChargingStart == null && GameStateManager.Instance.CurrentGameState == GameState.Aiming)
        {
            var aimingAngleVector = new Vector2(Mathf.Cos(aimingAngle), Mathf.Sin(aimingAngle));
            lineController.ShowTrajectory(target.transform.position, aimingAngleVector);
        }
    }

    public void RunDisableRoutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

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
        if (target != null) targetBall = target.GetComponent<DeterministicBall>();
        else targetBall = null;

        spriteRenderer.enabled = true;

        // Initialize aiming angle to point from cue -> target so rotation starts sensibly
        if (target != null)
        {
            Vector2 dir = (target.transform.position - transform.position);
            if (dir.sqrMagnitude > 1e-6f)
                aimingAngle = Mathf.Atan2(dir.y, dir.x);
        }

        // if the player is already holding space, start charging using unscaled time
        if (Input.GetKey(KeyCode.Space) && isChargingStart == null)
            isChargingStart = GetUnscaledTime();
    }

    private void HandleInput()
    {
        if (target == null) return;

        // --- MOUSE AIMING ---
        // Compute mouse world position and point the aim from TARGET -> MOUSE
        Camera cam = Camera.main ?? Camera.current;
        if (cam != null)
        {
            // if player inputs direction horizontal
            if(Input.GetAxisRaw("Horizontal") != 0f)
            {
                 fineAimingActive = true;
            }
            else if(previousMouseLocation != new Vector2(Input.mousePosition.x, Input.mousePosition.y))
            {
                fineAimingActive = false;
                horizontalNudgeAmount = 0f;
            }

            if (fineAimingActive)
            {
                //use arrow keys to nudge aiming angle by a small amount
                if (Input.GetAxisRaw("Horizontal") == 1f) horizontalNudgeAmount++;
                else if (Input.GetAxisRaw("Horizontal") == -1f) horizontalNudgeAmount--;
                Vector3 mouseWorld3 = cam.ScreenToWorldPoint(new Vector3(previousMouseLocation.x, previousMouseLocation.y, cam.nearClipPlane));
                Vector2 clampedMousePosition = new Vector2(mouseWorld3.x, mouseWorld3.y);
                Vector2 targetPos = target.transform.position;
                Vector2 dir = clampedMousePosition - targetPos;
                if (dir.sqrMagnitude > 1e-8f)
                {
                    aimingAngle = Mathf.Atan2(dir.y + horizontalNudgeAmount, dir.x + horizontalNudgeAmount);
                }
            }
            else
            {
                previousMouseLocation = Input.mousePosition;
                Vector3 mouseWorld3 = cam.ScreenToWorldPoint(new Vector3(previousMouseLocation.x, previousMouseLocation.y, cam.nearClipPlane));
                // We only care about x,y plane
                Vector2 clampedMousePosition = new Vector2(mouseWorld3.x, mouseWorld3.y);
                Vector2 targetPos = target.transform.position;
                Vector2 dir = clampedMousePosition - targetPos;
                if (dir.sqrMagnitude > 1e-8f)
                {
                    aimingAngle = Mathf.Atan2(dir.y, dir.x);
                }

            }
        }

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
                targetBall.Shoot(aimingAngle, finalStrength);
            }

            EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target });
            isChargingStart = null;
        }
    }

    private void SetPosition()
    {
        if (target == null) return;

        Vector2 targetWorld = target.transform.position;
        var offset = getOffset(distanceFromTarget - (chargeTime * 3f), aimingAngle);
        Vector2 cuePos = targetWorld + offset;
        transform.position = cuePos;

        // Rotate the cue to face the aim direction
        transform.rotation = Quaternion.Euler(0, 0, aimingAngle * Mathf.Rad2Deg);
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
