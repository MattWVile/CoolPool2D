using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class CueMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float distanceFromTarget = -4f; // Distance of the cue from the cue ball
    public GameObject target; // the target ball
    private Shootable targetShootable;
    public float AimingAngle;

    public float smoothness = 10f;

    public float shotStrength = 1f;

    private float? isChargingStart = null;

    private float chargeTime => isChargingStart != null ? Mathf.Clamp(Time.time - isChargingStart.Value, 0, 1) : 0;

    private void Update()
    {
        SetPosition();
        HandleInput();
    }

    private void HandleInput()
    {
        // move the cue left and right
        float cueMovement = Input.GetAxis("Horizontal");
        AimingAngle += cueMovement * Time.deltaTime;

        if (Input.GetKey(KeyCode.Space))
        {
            if (isChargingStart != null) return;
            isChargingStart = Time.time;
        }
        else
        {
            if (isChargingStart == null) return;
            var power = Mathf.Clamp(chargeTime, 0, 1);
            targetShootable.Shoot(AimingAngle, power * shotStrength);
            isChargingStart = null;
            EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target });
        }
    }

    public IEnumerator Disable(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        target = null;
        targetShootable = null;
        spriteRenderer.enabled = false;
    }

    public void Enable(GameObject targetObj)
    {
        target = targetObj;
        targetShootable = target.GetComponent<Shootable>();
        spriteRenderer.enabled = true;
    }

    private void SetPosition()
    {
        var smoothness = 10f;
        if (target == null)
            return;
        var offset = getOffset(distanceFromTarget - (chargeTime * 5f), AimingAngle);
        Vector3 targetPosition = (Vector2)target.transform.position + offset;

        // Smoothly move the cue to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothness);

        // Rotate the cue to face the direction the ball is going to move in
        transform.rotation = Quaternion.Euler(0, 0, AimingAngle * Mathf.Rad2Deg);
    }

    public Vector2 getOffset(float distance, float angle)
    {
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }
}