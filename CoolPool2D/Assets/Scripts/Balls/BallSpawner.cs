using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting;

public class BallSpawner : MonoBehaviour
{
    private static readonly List<string> ballSpawnPattern = new List<string>
    {"Y", "R", "Y", "Y", "B", "R", "R", "Y", "R", "Y", "Y", "R", "R", "Y", "R"};
    public static void SpawnBallsInTriangle()
    {
        var firstBallOfLineVector = Vector3.zero;
        var ballRadius = Resources.Load("Prefabs/ObjectBall", typeof(GameObject)).GetComponent<SpriteRenderer>().bounds.size.x / 2;
        int ballIndex = 1;
        Bounds clothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
        var clothDimensionsVector = clothBounds.size;
        var clothCenterVector = clothBounds.center;
        clothCenterVector.x += clothDimensionsVector.x / 5;

        var ballSpawnVector = clothCenterVector;
        //this is the first ball of the line it is 3.45 times the radius of the ball away from the black ball
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
                SpawnBall(ballSpawnVector, ballIndex);
                ballIndex++;

            }
        }
    }

    private static void SpawnBall(Vector3 spawnPosition, int ballIndex)
    {
        GameObject ball = Instantiate(Resources.Load("Prefabs/ObjectBall"), spawnPosition, Quaternion.identity) as GameObject;
        if (ball == null) throw new InvalidOperationException("Ball is null.");
        if (ballIndex > 15) ballIndex = new System.Random().Next(1, 15);

        var ballTypeString = ballSpawnPattern[ballIndex - 1];

        switch (ballTypeString)
        {
            case "R":
                ball.tag = "RedBall";
                ball.GetComponent<SpriteRenderer>().color = Color.red;
                break;
            case "Y":
                ball.tag = "YellowBall";
                ball.GetComponent<SpriteRenderer>().color = Color.yellow;
                break;
            case "B":
                ball.tag = "BlackBall";
                ball.GetComponent<SpriteRenderer>().color = Color.black;
                break;
            default:
                throw new InvalidOperationException($"Unexpected ball type: {ballTypeString}");
        }
    }

}
