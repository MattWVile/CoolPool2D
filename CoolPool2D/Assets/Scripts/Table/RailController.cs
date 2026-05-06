using UnityEngine;

public class RailController : MonoBehaviour
{
    public RailLocation railLocation;

    public float ballCollidedWithRailPoints = 10f;


    public void PublishBallCollidedWithRailEvent(GameObject ballThatHitRail)
    {
        BallScoringData ballData = ballThatHitRail.gameObject.GetComponent<BallScoringData>();
        {
            var ballCollidedWithRailEvent = new BallCollidedWithRailEvent
            {
                BallData = ballData,
                RailLocation = railLocation,
                Sender = this,
                ScoreTypeHeader = " collided With Rail",
                ScoreTypePoints = ballCollidedWithRailPoints + ballData.BallPoints,
                IsFoul = false
            };
            EventBus.Publish(ballCollidedWithRailEvent);
        }
    }
}
