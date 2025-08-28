using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();
    private static readonly Dictionary<Type, List<Delegate>> _interfaceEvents = new Dictionary<Type, List<Delegate>>();

    public static void Subscribe<TGameEventArgs>(Action<TGameEventArgs> handler) where TGameEventArgs : IGameEventArgs
    {
        var eventType = typeof(TGameEventArgs);
        if (eventType.IsInterface)
        {
            if (!_interfaceEvents.ContainsKey(eventType))
            {
                _interfaceEvents[eventType] = new List<Delegate>();
            }
            _interfaceEvents[eventType].Add(handler);
        }
        else
        {
            if (_events.ContainsKey(eventType))
            {
                _events[eventType] = Delegate.Combine(_events[eventType], handler);
            }
            else
            {
                _events[eventType] = handler;
            }
        }
    }

    public static void Unsubscribe<TGameEventArgs>(Action<TGameEventArgs> handler) where TGameEventArgs : IGameEventArgs
    {
        var eventType = typeof(TGameEventArgs);
        if (eventType.IsInterface)
        {
            if (_interfaceEvents.ContainsKey(eventType))
            {
                _interfaceEvents[eventType].Remove(handler);
                if (_interfaceEvents[eventType].Count == 0)
                {
                    _interfaceEvents.Remove(eventType);
                }
            }
        }
        else
        {
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
    }

    public static void Publish<TGameEventArgs>(TGameEventArgs eventArgs) where TGameEventArgs : IGameEventArgs
    {
        var eventType = typeof(TGameEventArgs);
        if (_events.ContainsKey(eventType))
        {
            var handler = _events[eventType] as Action<TGameEventArgs>;
            handler?.Invoke(eventArgs);
        }

        foreach (var interfaceType in _interfaceEvents.Keys)
        {
            if (interfaceType.IsAssignableFrom(eventType))
            {
                foreach (var handler in _interfaceEvents[interfaceType])
                {
                    (handler as Action<TGameEventArgs>)?.Invoke(eventArgs);
                }
            }
        }
    }
}
