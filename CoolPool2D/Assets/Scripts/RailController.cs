using UnityEngine;

public class RailController : MonoBehaviour
{
    public Rail rail;

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(collision.gameObject, out Ball ball))
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
