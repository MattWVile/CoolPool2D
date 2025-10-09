
//using UnityEngine;

//public class RewindBallToStartingPosition : MonoBehaviour
//{
//    void Start()
//    {
//        EventBus.Subscribe<IScorableEvent>(OnScorableEvent);
//gameObject.GetComponent<BallData>().numberOfOnBallHitEffects++;
//    }

//    void OnDestroy()
//    {
//        EventBus.Unsubscribe<IScorableEvent>(OnScorableEvent);
//    }
//    public void OnScorableEvent(IScorableEvent @event)
//    {
//        switch (@event)
//        {
//            //case BallPocketedEvent e: HandleBallPocketedEvent(e); break;
//            //case BallCollidedWithRailEvent e: HandleBallCollidedWithRailEvent(e); break;
//            case BallKissedEvent kissedEvent: HandleBallKissedEvent(kissedEvent.BallData.gameObject, kissedEvent.CollisionBallData.gameObject); break;
//            default:
//                Debug.LogWarning($"Unhandled event type: {@event.GetType()}");
//                break;
//        }
//    }
//    public void HandleBallKissedEvent(GameObject self, GameObject other)
//    {
//        var selfBallData = self.GetComponent<BallData>();
//        DeterministicBall selfDeterministicBall = self.GetComponent<DeterministicBall>();

//        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn >= selfBallData.numberOfOnBallHitEffects) return;

//        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn++;
//    }
//}
