
using UnityEngine;

public class JawController : MonoBehaviour
{
    public JawLocation jawLocation;

    public float ballCollidedWithJawPoints = 50f;

    public void PublishBallCollidedWithJawEvent(GameObject ballThatHitJaw)
    {
        BallData ballData = ballThatHitJaw.gameObject.GetComponent<BallData>();
        {
            var ballCollidedWithJawEvent = new BallCollidedWithJawEvent
            {
                BallData = ballData,
                JawLocation = jawLocation,
                Sender = this,
                ScoreTypeHeader = " collided With Jaw",
                ScoreTypePoints = ballCollidedWithJawPoints + ballData.BallPoints,
                IsFoul = false
            };
            EventBus.Publish(ballCollidedWithJawEvent);
        }
    }
}
