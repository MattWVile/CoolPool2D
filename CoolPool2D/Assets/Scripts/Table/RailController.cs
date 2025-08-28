using UnityEngine;

public class RailController : MonoBehaviour
{
    public RailLocation railLocation;


    public void PublishBallCollidedWithRailEvent(GameObject ballThatHitRail)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(ballThatHitRail.gameObject, out BallData ball))
        {
            var ballCollidedWithRailEvent = new BallCollidedWithRailEvent
            {
                Ball = ball,
                RailLocation = railLocation,
                Sender = this,
                ScoreTypeHeader = " collided With Rail",
                ScoreTypePoints = ball.BallPoints,
                IsFoul = false
            };
            EventBus.Publish(ballCollidedWithRailEvent);
        }
    }
}
