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

    private float? isChargingStart = null;

    private float chargeTime => isChargingStart != null
        ? Mathf.Clamp(Time.time - isChargingStart.Value, 0, 1)
        : 0;

    private Vector2 initialTargetPosition;

    public float aimingSpeed = 1f; // Speed of aiming adjustment

    private void Update()
    {
        SetPosition();
        HandleInput();
        BallAimingLineController lineController = target.GetComponent<BallAimingLineController>();
        if (lineController != null && isChargingStart == null && GameStateManager.Instance.CurrentGameState == GameState.Aiming)
           {
                var aimingAngleVector = new Vector2(Mathf.Cos(AimingAngle), Mathf.Sin(AimingAngle));
            lineController.ShowTrajectory(target.transform.position, aimingAngleVector);
        }
    }

    private void HandleInput()
    {
        // Adjust aim speed
        if (Input.GetKeyDown(KeyCode.W))
        {
            aimingSpeed = Mathf.Min(aimingSpeed + 0.3f, 1f);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            aimingSpeed = Mathf.Max(aimingSpeed - 0.3f, 0.1f);
        }

        // Rotate aim
        float cueMovement = Input.GetAxis("Horizontal");
        AimingAngle += cueMovement * Time.deltaTime * aimingSpeed;

        // Charging shot
        if (Input.GetKey(KeyCode.Space))
        {
            if (isChargingStart != null) return;
            isChargingStart = Time.time;
        }
        else
        {
            if (isChargingStart == null) return;

            // Shoot the ball
            if (targetBall != null)
            {
                targetBall.Shoot(AimingAngle, shotStrength);
            }

            isChargingStart = null;
            EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target });
        }
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
        initialTargetPosition = target.transform.position;
        targetBall = target.GetComponent<DeterministicBall>();
        spriteRenderer.enabled = true;
    }

    private void SetPosition()
    {
        if (target == null) return;

        var offset = getOffset(distanceFromTarget - (chargeTime * 3f), AimingAngle);
        Vector2 targetPosition = initialTargetPosition + offset;
        transform.position = targetPosition;

        // Rotate the cue to face the aim direction
        transform.rotation = Quaternion.Euler(0, 0, AimingAngle * Mathf.Rad2Deg);
    }

    public Vector2 getOffset(float distance, float angle)
    {
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }
}
