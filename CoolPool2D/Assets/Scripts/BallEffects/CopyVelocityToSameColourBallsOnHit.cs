using UnityEngine;

public class CopyVelocityToSameColourBallsOnHit : MonoBehaviour
{
    void Start()
    {
        EventBus.Subscribe<BallKissedEvent>(OnBallKissedEvent);
        gameObject.GetComponent<BallData>().numberOfOnBallHitEffects++;
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<BallKissedEvent>(OnBallKissedEvent);
    }

    public void OnBallKissedEvent(BallKissedEvent ballKissedEvent)
    {
        GameObject selfGameObject = ballKissedEvent.CollisionBallData.gameObject;
        GameObject other = ballKissedEvent.BallData.gameObject;
        BallData selfBallData = ballKissedEvent.CollisionBallData;

        if (selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn >= selfBallData.numberOfOnBallHitEffects) return;

        if (other.GetComponent<BallData>().BallColour != BallColour.Cue) return;

        foreach (GameObject gameObject in GameManager.Instance.ballGameObjects)
        {
            BallData ballData = gameObject.GetComponent<BallData>();
            DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();

            if (ballData.BallColour == selfBallData.BallColour && gameObject != selfGameObject && gameObject != other)
            {
                deterministicBall.velocity = selfGameObject.GetComponent<DeterministicBall>().velocity;
            }
        }
        selfBallData.numberOfOnBallHitEffectsTriggeredThisTurn++;
    }
}