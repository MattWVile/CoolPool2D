using UnityEngine;

public class PocketController : MonoBehaviour
{
    public PocketLocation PocketLocation;

    public float radius = 1f;

    private float ballPocketedPoints = 500f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, radius);

    }

    public void PublishBallPocketedEvent(GameObject pocketedBall)
    {
        BallData ballData = pocketedBall.gameObject.GetComponent<BallData>();
        {
            var ballPocketedEvent = new BallPocketedEvent
            {
                BallData = ballData,
                PocketLocation = PocketLocation,
                Sender = this,
                ScoreTypeHeader = " pot",
                ScoreTypePoints = ballPocketedPoints + ballData.BallPoints,
                IsFoul = "CueBall" == pocketedBall.tag
            };
            EventBus.Publish(ballPocketedEvent);
        }
    }
}
