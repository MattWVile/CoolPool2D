using UnityEngine;

public class RailController : MonoBehaviour
{
    public Rail rail;

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(collision.gameObject, out Ball ball))
        {
            string scoreTypeHeader = GetScoreTypeHeader(ball);

            var ballCollidedWithRailEvent = new BallCollidedWithRailEvent
            {
                Ball = ball,
                Rail = rail,
                Sender = this,
                ScoreTypeHeader = scoreTypeHeader,
                ScoreTypePoints = ball.BallPoints,
                IsFoul = false
            };
            EventBus.Publish(ballCollidedWithRailEvent);
        }
    }

    private string GetScoreTypeHeader(Ball ball)
    {
        switch (ball.BallGameObject.tag)
        {
            case "YellowBall":
                return "Yellow Ball Rail Hit";
            case "RedBall":
                return "Red Ball Rail Hit";
            case "BlackBall":
                return "Black Ball Rail Hit";
            case "CueBall":
                return "Cue Ball Rail Hit";
            default:
                return "Unknown Ball Rail Hit";
        }
    }
}
