using UnityEngine;

public class CueMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float distance = -4f; // Distance of the cue from the cue ball
    public GameObject target; // the target ball
    private Shootable targetShootable;
    public float AimingAngle;

    public float shotStrength = 1f;

    private float? isChargingStart = null;

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
            float chargeTime = Time.time - isChargingStart.Value;
            var power = Mathf.Clamp(chargeTime, 0, 1);
            targetShootable.Shoot(AimingAngle, power * shotStrength);
            isChargingStart = null;
            EventBus.Publish(new BallHasBeenShotEvent { Sender = this, Target = target});
        }
    }

    public void Disable()
    {
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
        if(target == null) return;
        var offset = getOffset(distance, AimingAngle);
        transform.position = (Vector2) target.transform.position + offset;
        // rotate the cue to face the direction the ball is going to move in
        transform.rotation = Quaternion.Euler(0, 0, AimingAngle * Mathf.Rad2Deg);
    }

    public Vector2 getOffset(float distance, float angle)
    {
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }
}
