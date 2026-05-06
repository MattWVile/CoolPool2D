using UnityEngine;

public class CopyVelocityToSameColourBallsOnHit : BaseBallEffect<BallKissedEvent>
{
    protected override void OnEvent(BallKissedEvent ballKissedEvent)
    {
        GameObject selfGameObject = this.gameObject;

        GameObject otherGameObject = (ballKissedEvent.BallData.gameObject == selfGameObject)
        ? ballKissedEvent.CollisionBallData.gameObject 
        : ballKissedEvent.BallData.gameObject;

        if (otherGameObject.GetComponent<BallScoringData>().BallColour != BallColour.Cue) return;

        BallScoringData selfBallData = selfGameObject.GetComponent<BallScoringData>();
        foreach (GameObject gameObject in GameManager.Instance.ballGameObjects)
        {
            BallScoringData ballData = gameObject.GetComponent<BallScoringData>();
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();

            if (ballData.BallColour == selfBallData.BallColour && gameObject != selfGameObject && gameObject != otherGameObject)
            {
                deterministicBall.velocity = selfGameObject.GetComponent<DeterministicBall>().velocity;
            }
        }
        hasEffectTriggeredThisShot = true;
    }
}