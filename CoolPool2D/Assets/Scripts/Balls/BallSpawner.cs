using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public static class PocketLocations
{
    private static Dictionary<BallSpawnLocations, Vector2> pocketLocationVectors = new()
    {
        { BallSpawnLocations.InFrontOfToBottomCenterPocket, new Vector2(1.835f, -3f) },
        { BallSpawnLocations.InFrontOfToBottomLeftPocket, new Vector2(-4.478f, -3f) },
        { BallSpawnLocations.InFrontOfToBottomRightPocket, new Vector2(8.17f, -3f) },
        { BallSpawnLocations.InFrontOfToTopCenterPocket, new Vector2(1.835f, 3f) },
        { BallSpawnLocations.InFrontOfToTopLeftPocket, new Vector2(-4.478f, 3f) },
        { BallSpawnLocations.InFrontOfToTopRightPocket, new Vector2(8.17f, 3f) },
        { BallSpawnLocations.TriangleCenter, BallSpawner.TriangleCenter },
        { BallSpawnLocations.CueBallInitialPosition, BallSpawner.CueBallInitialPosition }
    };

    public static Vector2 GetPocketLocationVector(BallSpawnLocations location)
    {
        if (pocketLocationVectors.TryGetValue(location, out var vector))
        {
            return vector;
        }
        throw new ArgumentException($"No pocket location vector found for {location}");
    }
}
public enum BallSpawnLocations
{
    TriangleCenter,
    InFrontOfToBottomCenterPocket,
    InFrontOfToBottomLeftPocket,
    InFrontOfToBottomRightPocket,
    InFrontOfToTopCenterPocket,
    InFrontOfToTopLeftPocket,
    InFrontOfToTopRightPocket,
    CueBallInitialPosition,
    Random
}

public class BallSpawner : MonoBehaviour
{
    public static Vector2 CueBallInitialPosition = new(-1.91f, 0.0384333f);
    public static Bounds ClothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
    public static Vector2 ClothCenterVector = ClothBounds.center;
    public static Vector2 ClothDimensionsVector = ClothBounds.size;
    public static Vector2 TriangleCenter = new(ClothCenterVector.x + ClothDimensionsVector.x / 5, ClothCenterVector.y);

    public static void SpawnLastShotBalls(IReadOnlyList<BallSnapshot> ballsToSpawn)
    {
        foreach (var ballSnapshot in ballsToSpawn)
        {
            var ballGameObject = SpawnSpecificColourBall(ballSnapshot.Colour, BallSpawnLocations.Random);
            ballGameObject.transform.position = ballSnapshot.Position;
        }
    }

    public static void SpawnNextRoundBalls(IReadOnlyList<BallSnapshot> ballsToSpawn){
        if (ballsToSpawn == null || ballsToSpawn.Count == 0) 
            return;

        // Get ball radius using the same deterministic prefab as original method
        var deterministic = Resources.Load("Prefabs/DeterministicBall", typeof(GameObject)) as GameObject;
        if (deterministic == null)
        {
            Debug.LogError("DeterministicBall prefab not found - cannot calculate spacing.");
            return;
        }

        var ballRadius = deterministic.GetComponent<SpriteRenderer>().bounds.size.x / 2f;

        var firstBallOfLineVector = Vector2.zero;
        var ballSpawnVector = TriangleCenter;
        ballSpawnVector.x += ballRadius * 3.45f;

        var ballIndex = 0; // index into ballsToSpawn

        var ballList = ballsToSpawn.ToList();

        foreach (var ball in ballList.Where(ball => ball.Active && ball.Colour != BallColour.Cue)) // skip inactive and cue balls
        {
            var spawnPosition = GetBallPositionWithinTriangle(ballList.IndexOf(ball), ballRadius);
            var ballGameObject = SpawnSpecificColourBallWithVector(ball.Colour, spawnPosition);
            if (ballGameObject == null)
                Debug.LogError($"Failed to spawn ball for colour {ball.Colour} at triangle position {spawnPosition}");
        }
    }

    private static Vector2 GetBallPositionWithinTriangle(int ballIndex, float ballRadius) {
        // Determine which row (1-indexed): row 1 has 1 ball, row 2 has 2 balls, etc.
        int row = (int)Math.Ceiling((-1 + Math.Sqrt(1 + 8 * ballIndex)) / 2) + 1;

        // Position within the row (0-indexed)
        int firstIndexOfRow = (row - 1) * row / 2;
        int positionInRow = ballIndex - firstIndexOfRow;

        // Calculate base position
        Vector2 pos = new(){x = TriangleCenter.x, y = TriangleCenter.y };
        pos.x += ((ballRadius * 2) * row);                             
        pos.y += ((ballRadius * 2) * (row-1));
        pos.y -= (ballRadius * Math.Abs(positionInRow) * 2f);
        pos.y -= (ballRadius * row-1) + 1f;

        return pos;
    }

    public static GameObject SpawnSpecificColourBall(BallColour ballColour, BallSpawnLocations spawnPositionSelector, BallData specificBallData = null)
    {
        Vector2 spawnPosition;

        if (spawnPositionSelector == BallSpawnLocations.Random)
        {
            spawnPosition = GetRandomSpawnPosition();
        }
        else
        {
            spawnPosition = PocketLocations.GetPocketLocationVector(spawnPositionSelector);
        }

        if (ballColour == BallColour.Random)
        {
            ballColour = (BallColour)UnityEngine.Random.Range(1, Enum.GetNames(typeof(BallColour)).Length - 3);

        }

        var ballGameObject = Instantiate(Resources.Load($"Prefabs/{ballColour}Ball"), spawnPosition, Quaternion.identity) as GameObject;

        if (ballGameObject == null) throw new InvalidOperationException("ballGameObject is null.");

        if (specificBallData != null)
        {
            var ballData = ballGameObject.GetComponent<BallData>();
            ballData.ballColour = specificBallData.ballColour;
            ballData.ballPoints = specificBallData.ballPoints;
            ballData.ballMultiplier = specificBallData.ballMultiplier;
            ballData.numberOfOnBallHitEffectsTriggeredThisTurn = specificBallData.numberOfOnBallHitEffectsTriggeredThisTurn;
            ballData.numberOfOnBallHitEffects = specificBallData.numberOfOnBallHitEffects;
        }
        GameManager.Instance.AddBallToLists(ballGameObject);
        return ballGameObject;
    }

    public static GameObject SpawnSpecificColourBallWithVector(BallColour ballColour, Vector2 spawnPosition)
    {
        if (ballColour == BallColour.Random)
        {
            ballColour = (BallColour)UnityEngine.Random.Range(0, Enum.GetNames(typeof(BallColour)).Length - 3);
        }

        var ballGameObject = Instantiate(Resources.Load($"Prefabs/{ballColour}Ball"), spawnPosition, Quaternion.identity) as GameObject;

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
        var ballGameObject = Instantiate(Resources.Load("Prefabs/CueBall"), CueBallInitialPosition, Quaternion.identity) as GameObject;
        GameManager.Instance.amountOfCueBallsSpawned++;
        GameManager.Instance.AddBallToLists(ballGameObject);
        return ballGameObject;
    }
}
