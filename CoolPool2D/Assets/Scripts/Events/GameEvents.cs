using JetBrains.Annotations;
using System;
using System.Collections.Generic;
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
public class BallHasBeenShotEvent : BaseGameEvent
{
    public new CueMovement Sender { get; set; } // overridden Sender to specify the sender type
    public GameObject Target { get; set; }
}

public class BallStoppedEvent : BaseGameEvent
{
    public new DeterministicBall Sender { get; set; } // overridden Sender to specify the sender type
}

//public class BallIsBeingMovedEvent : BaseGameEvent
//{
//    public new BallController Sender { get; set; } // overridden Sender to specify the sender type
//}


public class BallCollidedWithRailEvent : BaseGameEvent, IScorableEvent
{
    public RailLocation RailLocation { get; set; }
    public new RailController Sender { get; set; }
    public BallData BallData { get; set; }
    public string ScoreTypeHeader { get; set; }
    public float ScoreTypePoints { get; set; }
    public bool IsFoul { get; set; }
}

public class BallPocketedEvent : BaseGameEvent, IScorableEvent
{
    public PocketLocation PocketLocation { get; set; }
    public new PocketController Sender { get; set; }
    public BallData BallData { get; set; }
    public string ScoreTypeHeader { get; set; }
    public float ScoreTypePoints { get; set; }
    public bool IsFoul { get; set; }
}

public class BallKissedEvent : BaseGameEvent, IScorableEvent
{
    public new PoolWorld Sender { get; set; }
    public BallData BallData { get; set; }
    public BallData CollisionBallData { get; set; }
    public string ScoreTypeHeader { get; set; }
    public float ScoreTypePoints { get; set; }
    public bool IsFoul { get; set; }
}

public class ShotScoreTypeUpdatedEvent : BaseGameEvent
{
    public new ScoreManager Sender { get; set; }        // optional typed sender

    public ScoreType ScoreType { get; set; } // snapshot for UI
}


public class ScoringFinishedEvent : BaseGameEvent
{
    public new ScoreManager Sender { get; set; } // overridden Sender to specify the sender type
    public float TotalScore { get; set; }
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

public class NewGameStateEvent : BaseGameEvent {
    public new GameStateManager Sender { get; set; } // overridden Sender to specify the sender type
    public GameState NewGameState { get; set; }
}
