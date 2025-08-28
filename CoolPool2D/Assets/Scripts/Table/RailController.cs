using UnityEngine;

public class RailController : MonoBehaviour
{
    public RailLocation railLocation;


    public void PublishBallCollidedWithRailEvent(GameObject ballThatHitRail)
    {
        BallData ballData = ballThatHitRail.gameObject.GetComponent<BallData>();
        {
            var ballCollidedWithRailEvent = new BallCollidedWithRailEvent
            {
                BallData = ballData,
                RailLocation = railLocation,
                Sender = this,
                ScoreTypeHeader = " collided With Rail",
                ScoreTypePoints = ballData.BallPoints,
                IsFoul = false
            };
            EventBus.Publish(ballCollidedWithRailEvent);
        }
    }
}
