using UnityEngine;
using System;

public abstract class BaseBallEffect<TEvent> : MonoBehaviour where TEvent : BaseGameEvent
{
    protected bool hasEffectTriggeredThisShot = false;
    private Action<TEvent> _handler;

    protected virtual void Start()
    {
        _handler = e => {
            if (!hasEffectTriggeredThisShot && ShouldApply(e))
            {
                OnEvent(e);
                hasEffectTriggeredThisShot = true;
            }
        };
        EventBus.Subscribe(_handler);
        EventBus.Subscribe<BallStoppedEvent>(OnBallStoppedEventInternal);
    }

    protected virtual void OnDestroy()
    {
        EventBus.Unsubscribe(_handler);
        EventBus.Unsubscribe<BallStoppedEvent>(OnBallStoppedEventInternal);
    }

    // Override this for your effect logic
    protected abstract void OnEvent(TEvent e);

    // Default filtering: only apply if this ball is involved in BallKissedEvent
    protected virtual bool ShouldApply(TEvent e)
    {
        if (e is BallKissedEvent kissedEvent)
        {
            return this.gameObject == kissedEvent.BallData?.gameObject ||
                   this.gameObject == kissedEvent.CollisionBallData?.gameObject;
        }
        return true; // For other event types, always apply
    }

    private void OnBallStoppedEventInternal(BallStoppedEvent evt)
    {
        OnBallStoppedEvent(evt);
    }

    protected virtual void OnBallStoppedEvent(BallStoppedEvent evt) { }
}