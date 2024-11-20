using System;
using JetBrains.Annotations;
using UnityEngine;

public abstract class BaseGameEvent : IGameEventArgs
{
    public DateTime TimeStamp { get; } = DateTime.Now;

    // Sender is virtual so that it can be overridden in derived classes
    // This is useful when you want to statically type the sender in the event
    [CanBeNull] public virtual object Sender { get; set; }
}

public class GenericGameEvent : BaseGameEvent
{
    public string Message { get; set; }
}

public class BallStoppedEvent : BaseGameEvent
{
    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
}
public class BallHasBeenShotEvent : BaseGameEvent
{
    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
}
public class BallIsBeingMovedEvent : BaseGameEvent
{
    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
}
public class BallCollidedWithRailEvent : BaseGameEvent
{
    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
    public Collision Collision { get; set; } // overridden Sender to specify the sender type
}

//public class BallEnteredPocketEvent : BaseGameEvent
//{
//    public new PocketController Sender { get; set; } // overridden Sender to specify the sender type
//    public Collision Collision { get; set; }
//}

//public class ShotScoreCalculatedEvent : BaseGameEvent
//{
//    public new ScoreController Sender { get; set; } // overridden Sender to specify the sender type
//}

public class BallIsBeingChargedEvent : BaseGameEvent
{
    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
}
public class BallIsChargedEvent : BaseGameEvent
{
    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
}