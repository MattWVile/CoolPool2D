public enum PlayerBallColour { 
    None, 
    Red, 
    Yellow, 
    Black 
}

public enum Rail
{
    TopLeft,
    TopRight,
    MiddleLeft,
    MiddleRight,
    BottomLeft,
    BottomRight
}
public enum Pocket
{
    TopLeft,
    TopRight,
    TopMiddle,
    BottomMiddle,
    BottomLeft,
    BottomRight
}

public enum GameState
{
    Aiming,
    Shooting,
    CalculatePoints,
    PrepareNextTurn,
    GameStart,
    GameOver
}