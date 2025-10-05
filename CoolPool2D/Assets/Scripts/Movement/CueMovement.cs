using System.Collections;
using UnityEngine;

public class CueMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float distanceFromTarget = 4f; // Distance of the cue from the cue ball
    public GameObject targetGameObject; // The target ball
    public DeterministicBall targetBall; // The deterministic ball script
    public float aimingAngle;
    public bool fineAimingActive = false;

    private Vector2 previousMouseLocation;

    public float shotStrength = 1f;

    private float horizontalNudgeAmount = 0f;

    // store unscaled charge start time (null => not charging)
    private float? isChargingStart = null;

    bool isSecondaryHit = true;   
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
        if (targetGameObject == null) return;

        BallAimingLineController lineController = targetGameObject.GetComponent<BallAimingLineController>();
        if (lineController != null && isChargingStart == null && GameStateManager.Instance.CurrentGameState == GameState.Aiming)
        {
            var aimingAngleVector = new Vector2(Mathf.Cos(aimingAngle), Mathf.Sin(aimingAngle));
            lineController.ShowTrajectory(targetGameObject.transform.position, aimingAngleVector);
        }
    }

    public void RunDisableRoutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    public IEnumerator Disable(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        targetGameObject = null;
        targetBall = null;
        spriteRenderer.enabled = false;
    }

    public void Enable(GameObject targetObj, bool isInintialShot = true)
    {
        isSecondaryHit = isInintialShot;
        targetGameObject = targetObj;
        if (targetGameObject != null) targetBall = targetGameObject.GetComponent<DeterministicBall>();
        else targetBall = null;

        spriteRenderer.enabled = true;

        // Initialize aiming angle to point from cue -> target so rotation starts sensibly
        if (targetGameObject != null)
        {
            Vector2 dir = (targetGameObject.transform.position - transform.position);
            if (dir.sqrMagnitude > 1e-6f)
                aimingAngle = Mathf.Atan2(dir.y, dir.x);
        }

        // if the player is already holding space, start charging using unscaled time
        if (Input.GetKey(KeyCode.Space) && isChargingStart == null || Input.GetKey(KeyCode.Mouse0) && isChargingStart == null)
            isChargingStart = GetUnscaledTime();
    }

    private void HandleInput()
    {
        if (targetGameObject == null) return;

        Camera cam = Camera.main ?? Camera.current;
        if (cam != null)
        {
            Vector2 currentMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            // Detect keyboard input
            bool horizontalInput = Input.GetAxisRaw("Horizontal") != 0f;

            if (horizontalInput)
            {
                fineAimingActive = true;
            }
            else if (previousMouseLocation != currentMousePosition)
            {
                // Mouse moved – disable fine aiming
                fineAimingActive = false;
                horizontalNudgeAmount = 0f;
            }
            if (fineAimingActive)
            {
                float nudgeSpeed = 0.25f; // tweak to make it faster/slower

                // Nudge smoothly while key is held
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                    horizontalNudgeAmount += nudgeSpeed * Time.unscaledDeltaTime;
                else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                    horizontalNudgeAmount -= nudgeSpeed * Time.unscaledDeltaTime;

                // Use previous mouse position to preserve base aiming direction
                Vector3 mouseWorld3 = cam.ScreenToWorldPoint(new Vector3(previousMouseLocation.x, previousMouseLocation.y, cam.nearClipPlane));
                Vector2 clampedMousePosition = new Vector2(mouseWorld3.x, mouseWorld3.y);
                Vector2 targetPos = targetGameObject.transform.position;
                Vector2 dir = clampedMousePosition - targetPos;

                if (dir.sqrMagnitude > 1e-8f)
                {
                    aimingAngle = Mathf.Atan2(dir.y, dir.x + horizontalNudgeAmount);
                }
            }

            else
            {
                // Mouse aiming
                previousMouseLocation = currentMousePosition;

                Vector3 mouseWorld3 = cam.ScreenToWorldPoint(new Vector3(currentMousePosition.x, currentMousePosition.y, cam.nearClipPlane));
                Vector2 clampedMousePosition = new Vector2(mouseWorld3.x, mouseWorld3.y);
                Vector2 targetPos = targetGameObject.transform.position;
                Vector2 dir = clampedMousePosition - targetPos;

                if (dir.sqrMagnitude > 1e-8f)
                {
                    aimingAngle = Mathf.Atan2(dir.y, dir.x);
                }
            }
        }

        // Charging shot — use unscaled time to allow charging while frozen
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Mouse0))
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
                targetBall.Shoot(aimingAngle, finalStrength, targetGameObject, isSecondaryHit);
            }

            isChargingStart = null;
        }
    }

    private void SetPosition()
    {
        if (targetGameObject == null) return;

        Vector2 targetWorld = targetGameObject.transform.position;
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
