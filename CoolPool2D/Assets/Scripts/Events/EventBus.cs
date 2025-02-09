using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

    public static void Subscribe<TGameEventArgs>(Action<TGameEventArgs> handler) where TGameEventArgs : IGameEventArgs
    {
        var eventType = typeof(TGameEventArgs);
        if (_events.ContainsKey(eventType))
        {
            _events[eventType] = Delegate.Combine(_events[eventType], handler);
        }
        else
        {
            _events[eventType] = handler;
        }
    }

    public static void Unsubscribe<TGameEventArgs>(Action<TGameEventArgs> handler) where TGameEventArgs : IGameEventArgs
    {
        var eventType = typeof(TGameEventArgs);
        if (_events.ContainsKey(eventType))
        {
            var currentDelegate = _events[eventType];
            var newDelegate = Delegate.Remove(currentDelegate, handler);
            if (newDelegate == null)
            {
                _events.Remove(eventType);
            }
            else
            {
                _events[eventType] = newDelegate;
            }
        }
    }

    public static void Publish<TGameEventArgs>(TGameEventArgs eventArgs) where TGameEventArgs : IGameEventArgs
    {
        var eventType = typeof(TGameEventArgs);
        if (_events.ContainsKey(eventType))
        {
            var handler = _events[eventType] as Action<TGameEventArgs>;
            if (handler != null)
            {
                Debug.Log($"Publishing event of type {eventType}");
                handler.Invoke(eventArgs);
            }
            else
            {
                Debug.LogWarning($"No handler found for event of type {eventType}");
            }
        }
        else
        {
            Debug.LogWarning($"No subscribers for event of type {eventType}");
        }
    }
}
