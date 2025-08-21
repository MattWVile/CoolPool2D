using UnityEngine;

public class PocketController : MonoBehaviour
{
    public PocketLocation PocketLocation;

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
                PocketLocation = PocketLocation,
                Sender = this,
                ScoreTypeHeader = " pot",
                ScoreTypePoints = ball.BallPoints,
                IsFoul = "CueBall" == pocketedBall.tag
            };
            EventBus.Publish(ballPocketedEvent);
        }
    }
}
