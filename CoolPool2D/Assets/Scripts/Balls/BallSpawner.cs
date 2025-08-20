using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting;

public class BallSpawner : MonoBehaviour
{
    private static readonly List<string> ballSpawnPattern = new List<string>
    {"YellowBall", "RedBall", "YellowBall", "YellowBall", "BlackBall", "RedBall", "RedBall",
     "YellowBall", "RedBall", "YellowBall", "YellowBall", "RedBall", "RedBall", "YellowBall", "RedBall"};

    public static Vector2 cueBallInitialPosition = new Vector2(-1.91f, 0.0384333f);
    public static Bounds ClothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
    public static Vector2 ClothCenterVector = ClothBounds.center;
    public static Vector2 ClothDimensionsVector = ClothBounds.size;
    public static Vector2 TriangleCenterVector = new Vector2(ClothCenterVector.x + ClothDimensionsVector.x / 5, ClothCenterVector.y);

    public static Dictionary<GameObject, Ball> SpawnBallsInTriangle()
    {
        var balls = new Dictionary<GameObject, Ball>();
        var firstBallOfLineVector = Vector2.zero;
        var ballRadius = Resources.Load("Prefabs/ObjectBall", typeof(GameObject)).GetComponent<SpriteRenderer>().bounds.size.x / 2;
        int ballIndex = 1;

        var ballSpawnVector = TriangleCenterVector;
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
                var ball = SpawnTriangleBall(ballSpawnVector, ballIndex);
                balls.Add(ball.BallGameObject, ball);
                ballIndex++;
            }
        }
        return balls;
    }

    private static Ball SpawnTriangleBall(Vector2 spawnPosition, int ballIndex)
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

    public static Ball SpawnSpecificBall(String ballTypeString, String spawnPositionSelector)
    {
        Vector2 spawnPosition;
        switch (spawnPositionSelector)
        {
            case "Triangle Center":
                spawnPosition = TriangleCenterVector;
                break;
            case "Random":
                spawnPosition = GetRandomSpawnPosition();
                break;
            default:
                throw new InvalidOperationException($"Unexpected spawn position selector: {spawnPositionSelector}");
        }
        GameObject ballGameObject = Instantiate(Resources.Load("Prefabs/DeterministicBall"), spawnPosition, Quaternion.identity) as GameObject;

        if (ballGameObject == null) throw new InvalidOperationException("ballGameObject is null.");

        Ball ball = new Ball(ballTypeString, ballGameObject);

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


    public static Vector2 GetRandomSpawnPosition()
    {
        var ballRadius = Resources.Load("Prefabs/ObjectBall", typeof(GameObject)).GetComponent<SpriteRenderer>().bounds.size.x / 2;
        var clothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
        var clothDimensionsVector = clothBounds.size;
        var clothCenterVector = clothBounds.center;
        float xMin = clothCenterVector.x - (clothDimensionsVector.x / 2) + ballRadius;
        float xMax = clothCenterVector.x + (clothDimensionsVector.x / 2) - ballRadius;
        float yMin = clothCenterVector.y - (clothDimensionsVector.y / 2) + ballRadius;
        float yMax = clothCenterVector.y + (clothDimensionsVector.y / 2) - ballRadius;
        Vector2 spawnPosition = new Vector2(UnityEngine.Random.Range(xMin, xMax), UnityEngine.Random.Range(yMin, yMax));
        return spawnPosition;
    }

    public static Ball SpawnCueBall(int cueBallIndex)
    {
        GameObject ballGameObject = Instantiate(Resources.Load("Prefabs/DeterministicCueBall"), cueBallInitialPosition, Quaternion.identity) as GameObject;
        Ball ball = new Ball("CueBall" + " " + cueBallIndex, ballGameObject);
        ballGameObject.name = ball.BallName;
        ball.BallPoints = 0;
        return ball;
    }
}
