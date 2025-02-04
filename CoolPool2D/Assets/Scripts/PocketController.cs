using UnityEngine;

public class PocketController : MonoBehaviour
{
    public Pocket pocket;

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance.ballDictionary.TryGetValue(other.gameObject, out Ball ball))
        {
            EventBus.Publish(new BallPocketedEvent()
            {
                Ball = ball,
                Pocket = pocket,
                Sender = this
            });
        }
    }
}
