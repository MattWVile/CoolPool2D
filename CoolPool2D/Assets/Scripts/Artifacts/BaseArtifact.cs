using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ArtifactEffectType {
    Table,
    
}
public abstract class ScriptableArtifactBase : ScriptableObject {
    public abstract void ApplyEffect();
    public abstract void RemoveEffect();
    public string Name;
    public ArtifactEffectType EffectType;
}

public abstract class BaseArtifact<TEvent> : ScriptableArtifactBase where TEvent : BaseGameEvent {
    public string Name { get; set; }
    public ArtifactEffectType EffectType { get; set; }

    // Keep a reference to the handler so Unsubscribe uses the *same* delegate instance.
    private Action<TEvent> _handler;
    private bool _isSubscribed;

    // Your effect implementation for this specific event type
    protected abstract void OnEvent(TEvent e);

    // Optional filter hook
    protected virtual bool ShouldApply(TEvent e) => true;

    // Wiring
    public override void ApplyEffect() {
        if (_isSubscribed)
            return;
        _handler ??= e => {
            if (ShouldApply(e))
                OnEvent(e);
        };
        EventBus.Subscribe(_handler); // or EventBus.Subscribe<TEvent>(_handler);
        _isSubscribed = true;
    }

    public override void RemoveEffect() {
        if (!_isSubscribed)
            return;
        EventBus.Unsubscribe(_handler); // or EventBus.Unsubscribe<TEvent>(_handler);
        _isSubscribed = false;
    }
}
