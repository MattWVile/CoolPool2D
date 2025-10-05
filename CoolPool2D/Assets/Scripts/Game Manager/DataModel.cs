using UnityEngine;

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

public enum BallColour
{
    Blue,
    Purple,
    Orange,
    Black,
    Cue,
    Random
}

public enum MapNodeType
{
    Empty,
    Start,
    Shop,
    Treasure,
    RandomEvent,
    PoolEncounter,
    PoolBossEncounter
}
