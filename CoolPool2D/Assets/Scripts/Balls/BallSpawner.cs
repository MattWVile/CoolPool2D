using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting;

public class BallSpawner : MonoBehaviour
{
    private static readonly List<string> ballSpawnPattern = new List<string>
    {"YellowBall", "RedBall", "YellowBall", "YellowBall", "BlackBall", "RedBall", "RedBall",
     "YellowBall", "RedBall", "YellowBall", "YellowBall", "RedBall", "RedBall", "YellowBall", "RedBall"};

    public static Vector2 cueBallInitialPosition = new Vector2(-1.91f, 0.03f);
    public static Vector2 blackBallInitialPosition = new Vector2(4.29202366f, 0.0384333134f);

    public static Dictionary<GameObject, Ball> SpawnBallsInTriangle()
    {
        var balls = new Dictionary<GameObject, Ball>();
        var firstBallOfLineVector = Vector2.zero;
        var ballRadius = Resources.Load("Prefabs/ObjectBall", typeof(GameObject)).GetComponent<SpriteRenderer>().bounds.size.x / 2;
        int ballIndex = 1;
        Bounds clothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
        var clothDimensionsVector = clothBounds.size;
        var clothCenterVector = clothBounds.center;
        clothCenterVector.x += clothDimensionsVector.x / 5;

        var ballSpawnVector = clothCenterVector;
        ballSpawnVector.x += ballRadius * 3.45f;

        for (int ballRow = 1; ballRow < 6; ballRow++)
        {
            if (ballRow != 1)
            {
                firstBallOfLineVector.x += ballRadius * 1.73f;
            }

            for (int ballNumber = 1; ballNumber <= ballRow; ballNumber++)
            {
                if ((ballNumber == 1) && (ballRow != 1))
                {
                    firstBallOfLineVector.y += ballRadius;
                    ballSpawnVector = firstBallOfLineVector;
                }
                else if (ballRow != 1)
                {
                    ballSpawnVector.y -= ballRadius * 2;
                }
                else
                {
                    firstBallOfLineVector = ballSpawnVector;
                }
                var ball = SpawnBall(ballSpawnVector, ballIndex);
                balls.Add(ball.BallGameObject, ball);
                ballIndex++;
            }
        }
        return balls;
    }

    private static Ball SpawnBall(Vector2 spawnPosition, int ballIndex)
    {
        if (ballIndex > 15) ballIndex = new System.Random().Next(1, 15);
        var ballTypeString = ballSpawnPattern[ballIndex - 1];

        GameObject ballGameObject = Instantiate(Resources.Load("Prefabs/ObjectBall"), spawnPosition, Quaternion.identity) as GameObject;

        if (ballGameObject == null) throw new InvalidOperationException("ballGameObject is null.");

        Ball ball = new Ball(ballTypeString + " " + ballIndex.ToString(), ballGameObject);

        ballGameObject.tag = ballTypeString;
        ballGameObject.name = ball.BallName;
        switch (ballTypeString)
        {
            case "RedBall":
                ballGameObject.GetComponent<SpriteRenderer>().color = Color.red;
                break;
            case "YellowBall":
                ballGameObject.GetComponent<SpriteRenderer>().color = Color.yellow;
                break;
            case "BlackBall":
                ballGameObject.GetComponent<SpriteRenderer>().color = Color.black;
                ball.BallPoints = 500;
                break;
            default:
                throw new InvalidOperationException($"Unexpected ball type: {ballTypeString}");
        }
        return ball;
    }

    public static Ball SpawnCueBall(int cueBallIndex)
    {
        GameObject cueBallGameObject = Instantiate(Resources.Load("Prefabs/CueBall"), cueBallInitialPosition, Quaternion.identity) as GameObject;
        Ball ball = new Ball("CueBall" + " " + cueBallIndex, cueBallGameObject);
        cueBallGameObject.name = ball.BallName;
        return ball;
    }    
    
    public static Ball SpawnBlackBall()
    {
        GameObject blackBallGameObject = Instantiate(Resources.Load("Prefabs/ObjectBall"), blackBallInitialPosition, Quaternion.identity) as GameObject;
        Ball ball = new Ball("BlackBallReplacement", blackBallGameObject);
        blackBallGameObject.tag = "BlackBall";
        blackBallGameObject.name = ball.BallName;
        blackBallGameObject.GetComponent<SpriteRenderer>().color = Color.black;
        ball.BallPoints = 500;
        return ball;
    }
}
