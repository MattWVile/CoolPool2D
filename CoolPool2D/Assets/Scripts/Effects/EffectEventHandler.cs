using UnityEngine;

public class EffectEventHandler : MonoBehaviour
{

    void Start()
    {
        EventBus.Subscribe<IScorableEvent>(OnScorableEvent);
    }
    public void OnScorableEvent(IScorableEvent @event)
    {
        switch (@event)
        {
            //case BallPocketedEvent e: HandleBallPocketedEvent(e); break;
            //case BallCollidedWithRailEvent e: HandleBallCollidedWithRailEvent(e); break;
            case BallKissedEvent kissedEvent: HandleBallKissedEvent(kissedEvent); break;
            default:
                Debug.LogWarning($"Unhandled event type: {@event.GetType()}");
                break;
        }
    }
    private void HandleBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        ballKissedEvent.BallData.TriggerBallHitEffect(ballKissedEvent.CollisionBallData.gameObject);
        ballKissedEvent.CollisionBallData.TriggerBallHitEffect(ballKissedEvent.BallData.gameObject);
    }


}
