using System;
using UnityEngine;
public interface IScorableEvent : IGameEventArgs
{
    Ball Ball { get; set; }
    string ScoreTypeHeader { get; set; }
    float ScoreTypePoints { get; set; }
    bool IsFoul { get; set; }
}

public static class ScorableEventUtils
{
    public static string GetScoreTypeHeader(Ball ball)
    {
        switch (ball.BallGameObject.tag)
        {
            case "YellowBall":
                return "Yellow Ball";
            case "RedBall":
                return "Red Ball";
            case "BlackBall":
                return "Black Ball";
            case "CueBall":
                return "Cue Ball";
            default:
                throw new InvalidOperationException($"Unexpected ball tag: {ball.BallGameObject.tag}");
        }
    }

    public static bool DetermineIfFoul(Ball ball, PlayerBallColor playerColor)
    {
        PlayerBallColor ballColor;
        switch (ball.BallGameObject.tag)
        {
            case "YellowBall":
                ballColor = PlayerBallColor.Yellow;
                break;
            case "RedBall":
                ballColor = PlayerBallColor.Red;
                break;
            case "BlackBall":
                return true;
            case "CueBall":
                return true;
            default:
                throw new InvalidOperationException($"Unexpected ball tag: {ball.BallGameObject.tag}");
        }

        if (playerColor == PlayerBallColor.None)
        {
            // Assign the player's color based on the first ball
            GameManager.Instance.playerColor = ballColor;
            Debug.Log("Player's color is now: " + GameManager.Instance.playerColor);
            return false;
        }
        else
        {
            return ballColor != playerColor;
        }
    }
}
