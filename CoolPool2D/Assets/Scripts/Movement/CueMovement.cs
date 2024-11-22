using UnityEngine;
using System.Collections;

public class CueMovement : MonoBehaviour
{
    public float distance = -4f; // Distance of the cue from the cue ball

    private bool isBallBeingCharged = false;
    private bool hasBallBeenShot = false;

    private float distanceForCueToGoBack;
    private float distanceToGoBackMultiplier = 0.05f;
    private BallController cueBallControllerScript;
    private Vector2 cueBallPosition;

    private void Start()
    {
        cueBallControllerScript = GameObject.Find("CueBall").GetComponent<BallController>();
        cueBallPosition = cueBallControllerScript.transform.position;
        EventBus.Subscribe<BallHasBeenShotEvent>(OnBallHasBeenShotEvent);
        EventBus.Subscribe<BallIsBeingChargedEvent>(OnBallBeingChargedEvent);
        EventBus.Subscribe<BallStoppedEvent>(OnBallStoppedEvent);
    }

    private void Update()
    {
        float aimingAngle = cueBallControllerScript.aimingAngle;

        if (isBallBeingCharged)
        {
            transform.position = cueBallPosition + getOffset(distance - (distanceForCueToGoBack * distanceToGoBackMultiplier), aimingAngle);
        }
        else if (hasBallBeenShot)
        {
            var vectorToMoveTo = cueBallPosition + getOffset(distance + (distanceForCueToGoBack * distanceToGoBackMultiplier), aimingAngle);
            transform.position = vectorToMoveTo;
        }
        else
        {
            transform.position = cueBallPosition + getOffset(distance, aimingAngle);
        }
        transform.LookAt(cueBallPosition);
        transform.rotation = Quaternion.Euler(0, 0, aimingAngle * Mathf.Rad2Deg);
    }

    public Vector2 getOffset(float distance, float angle)
    {
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }
    private void OnBallBeingChargedEvent(BallIsBeingChargedEvent ballIsBeingChargedEvent)
    {
        isBallBeingCharged = true;
        hasBallBeenShot = false;
        distanceForCueToGoBack = ballIsBeingChargedEvent.Sender.amountOfForceToApplyToBall;
    }
    private void OnBallHasBeenShotEvent(BallHasBeenShotEvent ballHasBeenShotEvent)
    {
        hasBallBeenShot = true;
        isBallBeingCharged = false;
        StartCoroutine(DisableSpriteRendererWithDelay(0.2f));
    }

    public void OnBallStoppedEvent(BallStoppedEvent ballStoppedEvent)
    {
        if (gameObject.GetComponent<SpriteRenderer>().enabled) return;
        cueBallPosition = ballStoppedEvent.Sender.transform.position;
        gameObject.GetComponent<SpriteRenderer>().enabled = true;
    }

    private IEnumerator DisableSpriteRendererWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
    }
}
