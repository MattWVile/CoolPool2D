using UnityEngine;

public class CopyVelocityToSameColourBallsOnHit : BaseBallKissEffect
{
    protected override void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        if (hasEffectTriggeredThisShot) return;

        GameObject selfGameObject = ballKissedEvent.CollisionBallData.gameObject;
        GameObject other = ballKissedEvent.BallData.gameObject;
        BallData selfBallData = ballKissedEvent.CollisionBallData;

        if (other.GetComponent<BallData>().BallColour != BallColour.Cue) return;

        foreach (GameObject gameObject in GameManager.Instance.ballGameObjects)
        {
            BallData ballData = gameObject.GetComponent<BallData>();
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();

            if (ballData.BallColour == selfBallData.BallColour && gameObject != selfGameObject && gameObject != other)
            {
                deterministicBall.velocity = selfGameObject.GetComponent<DeterministicBall>().velocity;
            }
        }
        hasEffectTriggeredThisShot = true;
    }
}