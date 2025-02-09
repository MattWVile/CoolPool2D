using UnityEngine;

public class PocketController : MonoBehaviour
{
    public Pocket pocket;

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(other.gameObject, out Ball ball))
        {
            string scoreTypeHeader = GetScoreTypeHeader(ball);

            var ballPocketedEvent = new BallPocketedEvent
            {
                Ball = ball,
                Pocket = pocket,
                Sender = this,
                ScoreTypeHeader = scoreTypeHeader,
                ScoreTypePoints = ball.BallPoints,
                IsFoul = DetermineIfFoul(ball)
            };
            EventBus.Publish(ballPocketedEvent);
        }
    }

    private bool DetermineIfFoul(Ball ball)
    {
        GameManager.BallColor pocketedBallColor;
        switch (ball.BallGameObject.tag)
        {
            case "YellowBall":
                pocketedBallColor = GameManager.BallColor.Yellow;
                break;
            case "RedBall":
                pocketedBallColor = GameManager.BallColor.Red;
                break;
            case "BlackBall":
                return true;
            case "CueBall":
                return true;
            default:
                Debug.LogWarning("Unknown ball color pocketed.");
                return false;
        }

        if (GameManager.Instance.playerColor == GameManager.BallColor.None)
        {
            // Assign the player's color based on the first pocketed ball
            GameManager.Instance.playerColor = pocketedBallColor;
            Debug.Log("Player's color is now: " + GameManager.Instance.playerColor);
            return false;
        }
        else
        {
            return pocketedBallColor != GameManager.Instance.playerColor;
        }
    }

    private string GetScoreTypeHeader(Ball ball)
    {
        switch (ball.BallGameObject.tag)
        {
            case "YellowBall":
                return "Yellow Ball Pocketed";
            case "RedBall":
                return "Red Ball Pocketed";
            case "BlackBall":
                return "Black Ball Pocketed";
            case "CueBall":
                return "Cue Ball Pocketed";
            default:
                return "Unknown Ball Pocketed";
        }
    }
}
