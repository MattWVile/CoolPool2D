using UnityEngine;

public class CopyVelocityToSameColourBallsOnHit : BaseBallKissEffect
{
    protected override void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        GameObject selfGameObject = this.gameObject;

        GameObject otherGameObject = (ballKissedEvent.BallData.gameObject == selfGameObject)
        ? ballKissedEvent.CollisionBallData.gameObject 
        : ballKissedEvent.BallData.gameObject;

        if (otherGameObject.GetComponent<BallData>().BallColour != BallColour.Cue) return;

        BallData selfBallData = selfGameObject.GetComponent<BallData>();
        foreach (GameObject gameObject in GameManager.Instance.ballGameObjects)
        {
            BallData ballData = gameObject.GetComponent<BallData>();
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();

            if (ballData.BallColour == selfBallData.BallColour && gameObject != selfGameObject && gameObject != otherGameObject)
            {
                deterministicBall.velocity = selfGameObject.GetComponent<DeterministicBall>().velocity;
            }
        }
        hasEffectTriggeredThisShot = true;
    }
}