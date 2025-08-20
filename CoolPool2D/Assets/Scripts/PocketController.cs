using UnityEngine;

public class PocketController : MonoBehaviour
{
    public Pocket pocket;

    public float radius = 1f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, radius);

    }

    public void PublishBallPocketedEvent(GameObject pocketedBall)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(pocketedBall.gameObject, out Ball ball))
        {
            var ballPocketedEvent = new BallPocketedEvent
            {
                Ball = ball,
                Pocket = pocket,
                Sender = this,
                ScoreTypeHeader = "Ball Pocketed",
                ScoreTypePoints = ball.BallPoints,
                IsFoul = false
            };
            EventBus.Publish(ballPocketedEvent);
        }
    }
}
