using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.iOS;
using UnityEngine;

public enum BallSpawnLocations
{
    TriangleCenter,
    NextToLowCenterPocket,
    cueBallInitialPosition,
    Random
}

public class BallSpawner : MonoBehaviour
{
    private const int MAX_BALLS_IN_TRIANGLE = 15; // max balls the triangle layout supports (rows 1..5) = 15


    public static Vector2 cueBallInitialPosition = new Vector2(-1.91f, 0.0384333f);
    public static Bounds ClothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
    public static Vector2 ClothCenterVector = ClothBounds.center;
    public static Vector2 ClothDimensionsVector = ClothBounds.size;
    public static Vector2 TriangleCenterVector = new Vector2(ClothCenterVector.x + ClothDimensionsVector.x / 5, ClothCenterVector.y);
    private static Vector2 NextToLowCenterPocketVector = new Vector2(1.804565f, -2.938997f);

    public static void SpawnLastShotBalls(IReadOnlyList<BallSnapshot> ballsToSpawn)
    {

        foreach (var ballSnapshot in ballsToSpawn)
        {
            var ballGameObject = SpawnSpecificColourBall(ballSnapshot.Colour, BallSpawnLocations.Random);
            ballGameObject.transform.position = ballSnapshot.Position;
            if (ballGameObject == null)
            {
                Debug.LogError($"Failed to spawn ball for colour {ballSnapshot.Colour} at {ballSnapshot.Position}");
                continue;
            }
        }
    }


    public static void SpawnNextRoundBalls(IReadOnlyList<BallSnapshot> ballsToSpawn){
        if (ballsToSpawn == null || ballsToSpawn.Count == 0) 
            return;
        if (ballsToSpawn.Count > MAX_BALLS_IN_TRIANGLE)
            Debug.LogWarning($"SpawnNextRoundBalls: provided {ballsToSpawn.Count} snapshots, but triangle supports up to {MAX_BALLS_IN_TRIANGLE}. Only first {MAX_BALLS_IN_TRIANGLE} will be used.");
        

        var spawnLimit = Math.Min(ballsToSpawn.Count, MAX_BALLS_IN_TRIANGLE);

        // Get ball radius using the same deterministic prefab as original method
        var deterministic = Resources.Load("Prefabs/DeterministicBall", typeof(GameObject)) as GameObject;
        if (deterministic == null)
        {
            Debug.LogError("DeterministicBall prefab not found - cannot calculate spacing.");
            return;
        }

        var ballRadius = deterministic.GetComponent<SpriteRenderer>().bounds.size.x / 2f;

        var firstBallOfLineVector = Vector2.zero;
        var ballSpawnVector = TriangleCenterVector;
        ballSpawnVector.x += ballRadius * 3.45f;

        var ballIndex = 0; // index into ballsToSpawn

        var ballList = ballsToSpawn.ToList();

        foreach (var ball in ballList.Where(ball => ball.Active && ball.Colour != BallColour.Cue)) // skip inactive and cue balls
        {
            var spawnPosition = GetTrianglePosition(ballList.IndexOf(ball), ballRadius);
            var ballGameObject = SpawnSpecificColourBallWithVector(ball.Colour, spawnPosition);
            if (ballGameObject == null)
                Debug.LogError($"Failed to spawn ball for colour {ball.Colour} at triangle position {spawnPosition}");
        }




        //for (var ballRow = 1; ballRow < 6 && ballIndex < spawnLimit; ballRow++)
        //{
        //    if (ballRow != 1)
        //    {
        //        firstBallOfLineVector.x += ballRadius * 1.73f;
        //    }

        //    for (var ballNumber = 1; ballNumber <= ballRow && ballIndex < spawnLimit; ballNumber++)
        //    {
        //        //if ((ballNumber == 1) && (ballRow != 1))
        //        //{
        //        //    firstBallOfLineVector.y += ballRadius;
        //        //    ballSpawnVector = firstBallOfLineVector;
        //        //}
        //        //else if (ballRow != 1)
        //        //{
        //        //    ballSpawnVector.y -= ballRadius * 2f;
        //        //}
        //        //else
        //        //{
        //        //    firstBallOfLineVector = ballSpawnVector;
        //        //}

        //        var ballSnapshot = ballsToSpawn[ballIndex];
        //        ballIndex++;

        //        // if snapshot says the ball is not active, skip spawning it
        //        if (!ballSnapshot.Active)
        //        {
        //            continue;
        //        }

        //        var ballGameObject = ballSnapshot.Colour == BallColour.Cue
        //            ? SpawnCueBall(GameManager.Instance.amountOfCueBallsSpawned) // If the snapshot colour is Cue, use the cue-spawning path.
        //            : SpawnSpecificColourBallWithVector(ballSnapshot.Colour, GetTrianglePosition(ballNumber, ballRadius));
                
        //        if (ballGameObject == null)
        //            Debug.LogError($"Failed to spawn ball for colour {ballSnapshot.Colour} at triangle position {ballSpawnVector}");
        //    }
        //}
    }

    private static Vector2 GetTrianglePosition(int ballIndex, float ballRadius) {
        // Determine which row (1-indexed): row 1 has 1 ball, row 2 has 2 balls, etc.
        int row = (int)Math.Ceiling((-1 + Math.Sqrt(1 + 8 * ballIndex)) / 2) + 1;

        // Position within the row (0-indexed)
        int firstIndexOfRow = (row - 1) * row / 2;
        int positionInRow = ballIndex - firstIndexOfRow;

        // Calculate base position
        Vector2 pos = new Vector2();
        pos.x = TriangleCenterVector.x;
        pos.y = TriangleCenterVector.y;
        pos.x += ((ballRadius * 2) * row);                             
        pos.y += ((ballRadius * 2) * (row-1));
        pos.y -= (ballRadius * Math.Abs(positionInRow) * 2f);
        pos.y -= (ballRadius * (row - 1));

        return pos;
    }

    public static GameObject SpawnSpecificColourBall(BallColour ballColour, BallSpawnLocations spawnPositionSelector)
    {
        Vector2 spawnPosition;
        switch (spawnPositionSelector)
        {
            case BallSpawnLocations.cueBallInitialPosition:
                spawnPosition = cueBallInitialPosition;
                break;
            case BallSpawnLocations.TriangleCenter:
                spawnPosition = TriangleCenterVector;
                break;
            case BallSpawnLocations.NextToLowCenterPocket:
                spawnPosition = NextToLowCenterPocketVector;
                break;
            case BallSpawnLocations.Random:
                spawnPosition = GetRandomSpawnPosition();
                break;

            default:
                throw new InvalidOperationException($"Unexpected spawn position selector: {spawnPositionSelector}");
        }

        if(ballColour == BallColour.Random)
        {
            ballColour = (BallColour)UnityEngine.Random.Range(1, Enum.GetNames(typeof(BallColour)).Length - 3);

        }

        GameObject ballGameObject = Instantiate(Resources.Load($"Prefabs/{ballColour}Ball"), spawnPosition, Quaternion.identity) as GameObject;

        if (ballGameObject == null) throw new InvalidOperationException("ballGameObject is null.");
        GameManager.Instance.AddBallToLists(ballGameObject);
        return ballGameObject;
    }

    public static GameObject SpawnSpecificColourBallWithVector(BallColour ballColour, Vector2 spawnPosition)
    {
        if (ballColour == BallColour.Random)
        {
            ballColour = (BallColour)UnityEngine.Random.Range(1, Enum.GetNames(typeof(BallColour)).Length - 3);
        }

        GameObject ballGameObject = Instantiate(Resources.Load($"Prefabs/{ballColour}Ball"), spawnPosition, Quaternion.identity) as GameObject;

        if (ballGameObject == null) return null;
        GameManager.Instance.AddBallToLists(ballGameObject);
        return ballGameObject;
    }


    public static Vector2 GetRandomSpawnPosition()
    {
        var ballRadius = Resources.Load("Prefabs/DeterministicBall", typeof(GameObject)).GetComponent<SpriteRenderer>().bounds.size.x / 2;
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

    public static GameObject SpawnCueBall(int cueBallIndex)
    {
        GameObject ballGameObject = Instantiate(Resources.Load("Prefabs/CueBall"), cueBallInitialPosition, Quaternion.identity) as GameObject;
        GameManager.Instance.amountOfCueBallsSpawned++;
        GameManager.Instance.AddBallToLists(ballGameObject);
        return ballGameObject;
    }
}
