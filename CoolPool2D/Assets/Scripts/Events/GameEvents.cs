using System;
using JetBrains.Annotations;
using UnityEngine;

public interface IGameEventArgs { }
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

//public class BallStoppedEvent : BaseGameEvent
//{
//    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
//}
public class BallHasBeenShotEvent : BaseGameEvent
{
    public new CueMovement Sender { get; set; } // overridden Sender to specify the sender type
    public GameObject Target { get; set; }
}
//public class BallIsBeingMovedEvent : BaseGameEvent
//{
//    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
//}
public class BallCollidedWithRailEvent : BaseGameEvent, IScorableEvent
{
    public Rail Rail { get; set; }
    public new RailController Sender { get; set; }
    public Ball Ball { get; set; }
    public string ScoreTypeHeader { get; set; }
    public float ScoreTypePoints { get; set; }
    public bool IsFoul { get; set; }
}

//public class ShotScoreCalculatedEvent : BaseGameEvent
//{
//    public new ScoreController Sender { get; set; } // overridden Sender to specify the sender type
//}

//public class BallIsBeingChargedEvent : BaseGameEvent
//{
//    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
//}
//public class BallIsChargedEvent : BaseGameEvent
//{
//    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
//}

public class BallPocketedEvent : BaseGameEvent, IScorableEvent
{
    public Pocket Pocket { get; set; }
    public new PocketController Sender { get; set; }
    public Ball Ball { get; set; }
    public string ScoreTypeHeader { get; set; }
    public float ScoreTypePoints { get; set; }
    public bool IsFoul { get; set; }
}

public class NewGameStateEvent : BaseGameEvent {
    public new GameStateManager Sender { get; set; } // overridden Sender to specify the sender type
    public GameState NewGameState { get; set; }
}
