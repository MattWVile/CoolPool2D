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
    GameOver
}

public enum BallColour
{
    Red,
    Yellow,
    Blue,
    Purple,
    Maroon,
    Green,
    Orange,
    Black,
    Cue
}
