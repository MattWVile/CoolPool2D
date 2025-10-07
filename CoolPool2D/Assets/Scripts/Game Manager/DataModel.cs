using System;

public enum RailLocation {
    NoRail,
    TopLeft,
    TopRight,
    MiddleLeft,
    MiddleRight,
    BottomLeft,
    BottomRight
}

public enum JawLocation {
    NoJaw,
    TopLeftPocket,
    TopRightPocket,
    TopMiddlePocket,
    BottomMiddlePocket,
    BottomLeftPocket,
    BottomRightPocket
}

public enum PocketLocation {
    NoPocket,
    TopLeft,
    TopRight,
    TopMiddle,
    BottomMiddle,
    BottomLeft,
    BottomRight
}

public enum GameState {
    Aiming,
    Shooting,
    CalculatePoints,
    PrepareNextTurn,
    GameStart,
    GameOver,
    PrepareNextLevel
}

[Serializable]
public enum BallColour
{
    Blue,
    Purple,
    Orange,
    Black,
    Cue,
    Random
}

[Serializable]
public enum MapNodeType
{
    Empty,
    PartOfPath,
    Start,
    Shop,
    Treasure,
    RandomEvent,
    PoolEncounter,
    PoolBossEncounter
}
