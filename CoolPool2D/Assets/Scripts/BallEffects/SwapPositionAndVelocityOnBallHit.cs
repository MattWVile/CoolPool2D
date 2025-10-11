using UnityEngine;

public class SwapPositionAndVelocityOnBallHit : BaseBallKissEffect 
{

    protected override void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {

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
