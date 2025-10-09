using UnityEngine;

public abstract class BaseBallKissEffect : MonoBehaviour
{
    protected bool hasEffectTriggeredThisShot = false;

    protected virtual void Start()
    {
        EventBus.Subscribe<BallKissedEvent>(OnBallKissedEventInternal);
        EventBus.Subscribe<BallStoppedEvent>(OnBallStoppedEventInternal);
    }

    protected virtual void OnDestroy()
    {
        EventBus.Unsubscribe<BallKissedEvent>(OnBallKissedEventInternal);
        EventBus.Unsubscribe<BallStoppedEvent>(OnBallStoppedEventInternal);
    }

    // Internal handler to call your custom effect
    private void OnBallKissedEventInternal(BallKissedEvent evt)
    {
        if (!hasEffectTriggeredThisShot)
        {
            OnBallKissedEvent(evt);
            hasEffectTriggeredThisShot = true;
        }
    }

    // Resets the effect trigger on BallStoppedEvent
    private void OnBallStoppedEventInternal(BallStoppedEvent evt)
    {
        hasEffectTriggeredThisShot = false;
        OnBallStoppedEvent(evt);
    }

    // Override this in your effect class
    protected abstract void OnBallKissedEvent(BallKissedEvent evt);

    // Optionally override for custom BallStoppedEvent logic
    protected virtual void OnBallStoppedEvent(BallStoppedEvent evt) { }
}