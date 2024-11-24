using UnityEngine;
using System.Collections;

public class CueMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public GameObject target; // the target ball
    
    public float shotStrength = 1f;
    public float aimingAngle;

    public Vector2 targetPosition;

    private float distanceFromTarget = -4f; // Distance of the cue from target
    private float distanceForCueToGoBack = 1f;
    private float chargeTime;

    private float? isChargingStart = null;

    private Shootable targetShootable;

    private bool isBallBeingCharged = false;
    private bool hasBallBeenShot = false;

    private void Start()
    {
        EventBus.Subscribe<BallStoppedEvent>((@event) =>
        {
            isBallBeingCharged = false;
            hasBallBeenShot = false;
        });
    }
    private void Update()
    {
        SetPosition();
        HandleInput();
    }

    private void HandleInput()
    {
        // move the cue left and right
        float cueMovement = Input.GetAxis("Horizontal");
        aimingAngle += cueMovement * Time.deltaTime;

        if (Input.GetKey(KeyCode.Space))
        {
            if (isChargingStart == null){
                isChargingStart = Time.time;
                isBallBeingCharged = true;
                hasBallBeenShot = false;
            }
            else
            {
                chargeTime = Mathf.Clamp(Time.time - isChargingStart.Value, 0, 1);
            }
        }
        else
        {
            if (isChargingStart == null) return;
            isBallBeingCharged = false;
            hasBallBeenShot = true;
            chargeTime = Time.time - isChargingStart.Value;
            var power = Mathf.Clamp(chargeTime, 0, 1);
            targetShootable.Shoot(aimingAngle, power * shotStrength);
            isChargingStart = null;
            EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target });
        }
    }
    public IEnumerator DisableWithDelay(float delay)
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
        targetPosition = target.transform.position;
    }

    private void SetPosition()
    {
        if(target == null) return;
        if (isBallBeingCharged)
        {
            transform.position = targetPosition + getOffset(distanceFromTarget - (distanceForCueToGoBack * chargeTime), aimingAngle);
        }
        else if (hasBallBeenShot)
        {
            transform.position = targetPosition + getOffset(distanceFromTarget + (distanceForCueToGoBack), aimingAngle);
        }
        else
        {
            var offset = getOffset(distanceFromTarget, aimingAngle);
            transform.position = targetPosition + offset;
        }
        transform.rotation = Quaternion.Euler(0, 0, aimingAngle * Mathf.Rad2Deg);
    }

    public Vector2 getOffset(float distance, float angle)
    {
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }

}
