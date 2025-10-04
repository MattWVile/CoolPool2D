using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        if (ballsToSpawn == null || ballsToSpawn.Count == 0) return;

        // max balls the triangle layout supports (rows 1..5) = 15
        const int maxTriangleBalls = 15;
        if (ballsToSpawn.Count > maxTriangleBalls)
        {
            Debug.LogWarning($"SpawnNextRoundBalls: provided {ballsToSpawn.Count} snapshots, but triangle supports up to {maxTriangleBalls}. Only first {maxTriangleBalls} will be used.");
        }

        int spawnLimit = Math.Min(ballsToSpawn.Count, maxTriangleBalls);

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

        int ballIndex = 0; // index into ballsToSpawn

        for (int ballRow = 1; ballRow < 6 && ballIndex < spawnLimit; ballRow++)
        {
            if (ballRow != 1)
            {
                firstBallOfLineVector.x += ballRadius * 1.73f;
            }

            for (int ballNumber = 1; ballNumber <= ballRow && ballIndex < spawnLimit; ballNumber++)
            {
                if ((ballNumber == 1) && (ballRow != 1))
                {
                    firstBallOfLineVector.y += ballRadius;
                    ballSpawnVector = firstBallOfLineVector;
                }
                else if (ballRow != 1)
                {
                    ballSpawnVector.y -= ballRadius * 2f;
                }
                else
                {
                    firstBallOfLineVector = ballSpawnVector;
                }

                var ballSnapshot = ballsToSpawn[ballIndex];
                ballIndex++;

                // if snapshot says the ball is not active, skip spawning it
                if (!ballSnapshot.Active)
                {
                    continue;
                }

                GameObject ballGameObject = null;

                // If the snapshot colour is Cue, use the cue-spawning path.
                if (ballSnapshot.Colour == BallColour.Cue)
                {
                    ballGameObject = SpawnCueBall(GameManager.Instance.amountOfCueBallsSpawned); 
                }
                else
                {
                    // Spawn the requested colour at the computed triangle position
                    ballGameObject = SpawnSpecificColourBallWithVector(ballSnapshot.Colour, ballSpawnVector);
                }

                if (ballGameObject == null)
                {
                    Debug.LogError($"Failed to spawn ball for colour {ballSnapshot.Colour} at triangle position {ballSpawnVector}");
                    continue;
                }
            }
        }
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
