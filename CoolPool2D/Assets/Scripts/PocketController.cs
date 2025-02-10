using UnityEngine;

public class PocketController : MonoBehaviour
{
    public Pocket pocket;

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(other.gameObject, out Ball ball))
        {
            string scoreTypeHeader = ScorableEventUtils.GetScoreTypeHeader(ball) + " Pocketed";
            bool isFoul = ScorableEventUtils.DetermineIfFoul(ball, GameManager.Instance.playerColor);

            var ballPocketedEvent = new BallPocketedEvent
            {
                Ball = ball,
                Pocket = pocket,
                Sender = this,
                ScoreTypeHeader = scoreTypeHeader,
                ScoreTypePoints = ball.BallPoints,
                IsFoul = isFoul
            };
            EventBus.Publish(ballPocketedEvent);
        }
    }
}
