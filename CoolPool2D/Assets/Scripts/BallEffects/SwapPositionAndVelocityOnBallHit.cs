using UnityEngine;

public class SwapPositionAndVelocityOnBallHit : MonoBehaviour 
{
    public bool hasEffectTriggeredThisShot = false;
    void Start()
    {
        EventBus.Subscribe<BallKissedEvent>(OnBallKissedEvent);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<BallKissedEvent>(OnBallKissedEvent);
    }

    public void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        if (hasEffectTriggeredThisShot) return;

        GameObject otherGameObject = ballKissedEvent.BallData.gameObject;
        GameObject selfGameObject = ballKissedEvent.CollisionBallData.gameObject;
        BallData selfBallData = ballKissedEvent.CollisionBallData;

        DeterministicBall selfDeterministicBall = selfGameObject.GetComponent<DeterministicBall>();
        DeterministicBall otherDeterministicBall = otherGameObject.GetComponent<DeterministicBall>();

        selfGameObject.transform.position = selfDeterministicBall.stationaryPosition;
        otherDeterministicBall.velocity = selfDeterministicBall.velocity;

        otherGameObject.transform.position = otherDeterministicBall.stationaryPosition;
        selfDeterministicBall.velocity = otherDeterministicBall.initialVelocity * .9f;

        hasEffectTriggeredThisShot = true;
    }
}
