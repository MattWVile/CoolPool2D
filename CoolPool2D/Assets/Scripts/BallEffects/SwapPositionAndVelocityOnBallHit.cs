using UnityEngine;

public class SwapPositionAndVelocityOnBallHit : MonoBehaviour 
{ 
    void Start()
    {
        EventBus.Subscribe<BallKissedEvent>(OnBallKissedEvent);
        gameObject.GetComponent<BallData>().numberOfOnBallHitEffects++;
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<BallKissedEvent>(OnBallKissedEvent);
    }

    public void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        GameObject otherGameObject = ballKissedEvent.BallData.gameObject;
        GameObject selfGameObject = ballKissedEvent.CollisionBallData.gameObject;
        BallData selfBallData = ballKissedEvent.CollisionBallData;

        DeterministicBall selfDeterministicBall = selfGameObject.GetComponent<DeterministicBall>();
        DeterministicBall otherDeterministicBall = otherGameObject.GetComponent<DeterministicBall>();

        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn == selfBallData.numberOfOnBallHitEffects)
            return;

        selfGameObject.transform.position = selfDeterministicBall.stationaryPosition;
        otherDeterministicBall.velocity = selfDeterministicBall.velocity;

        otherGameObject.transform.position = otherDeterministicBall.stationaryPosition;
        selfDeterministicBall.velocity = otherDeterministicBall.initialVelocity * .9f;

        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn ++;
    }
}
