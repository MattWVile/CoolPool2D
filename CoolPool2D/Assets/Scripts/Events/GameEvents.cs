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
    public new CueMovement Sender { get; set; } // overridden Sender to specify the sender type
    public GameObject Target { get; set; }
}
public class BallIsBeingMovedEvent : BaseGameEvent
{
    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
}
public class BallCollidedWithRailEvent : BaseGameEvent
{
    public new RailController Sender { get; set; } // overridden Sender to specify the sender type
    public Rail Rail { get; set; }
    public GameObject Ball { get; set; }
}

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

public class BallPocketedEvent : BaseGameEvent
{
    public new PocketController Sender { get; set; } // overridden Sender to specify the sender type
    public Pocket Pocket { get; set; }
    public GameObject Ball { get; set; }
}

public class NewGameStateEvent : BaseGameEvent {
    public new GameStateManager Sender { get; set; } // overridden Sender to specify the sender type
    public GameState NewGameState { get; set; }
}
