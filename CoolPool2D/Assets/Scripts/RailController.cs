using UnityEngine;

public class RailController : MonoBehaviour
{
    public Rail rail;

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(collision.gameObject, out Ball ball))
        {
            EventBus.Publish(new BallCollidedWithRailEvent
            {
                Sender = this,
                Rail = rail,
                Ball = ball
            });
        }
    }
}
