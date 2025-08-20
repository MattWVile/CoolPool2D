using UnityEngine;

public class RailColliderMarker : MonoBehaviour
{
    public Rail rail;


    public void PublishBallCollidedWithRailEvent(GameObject ballThatHitRail)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(ballThatHitRail.gameObject, out Ball ball))
        {
            var ballCollidedWithRailEvent = new BallCollidedWithRailEvent
            {
                Ball = ball,
                Rail = rail,
                Sender = this,
                ScoreTypeHeader = "Ball Collided With Rail",
                ScoreTypePoints = ball.BallPoints,
                IsFoul = false
            };
            EventBus.Publish(ballCollidedWithRailEvent);
        }
    }
}
