using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public enum BallSpawnLocations
{
    TriangleCenter,
    NextToLowCenterPocket,
    Random
}
public class BallSpawner : MonoBehaviour
{
    //private static readonly List<BallColour> ballSpawnPattern = new List<BallColour>
    //{BallColour.Red, BallColour.Yellow, BallColour.Blue, BallColour.Purple, BallColour.Orange, BallColour.Maroon, BallColour.Green,
    // BallColour.Red, BallColour.Yellow, BallColour.Blue, BallColour.Purple, BallColour.Orange, BallColour.Maroon, BallColour.Green};

    public static Vector2 cueBallInitialPosition = new Vector2(-1.91f, 0.0384333f);
    public static Bounds ClothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
    public static Vector2 ClothCenterVector = ClothBounds.center;
    public static Vector2 ClothDimensionsVector = ClothBounds.size;
    public static Vector2 TriangleCenterVector = new Vector2(ClothCenterVector.x + ClothDimensionsVector.x / 5, ClothCenterVector.y);
    private static Vector2 NextToLowCenterPocketVector = new Vector2(1.804565f, -2.938997f);

    //public static Dictionary<GameObject, BallData> SpawnBallsInTriangle()
    //{
    //    var balls = new Dictionary<GameObject, BallData>();
    //    var firstBallOfLineVector = Vector2.zero;
    //    var ballRadius = Resources.Load("Prefabs/ObjectBall", typeof(GameObject)).GetComponent<SpriteRenderer>().bounds.size.x / 2;
    //    int ballIndex = 1;

    //    var ballSpawnVector = TriangleCenterVector;
    //    ballSpawnVector.x += ballRadius * 3.45f;

    //    for (int ballRow = 1; ballRow < 6; ballRow++)
    //    {
    //        if (ballRow != 1)
    //        {
    //            firstBallOfLineVector.x += ballRadius * 1.73f;
    //        }

    //        for (int ballNumber = 1; ballNumber <= ballRow; ballNumber++)
    //        {
    //            if ((ballNumber == 1) && (ballRow != 1))
    //            {
    //                firstBallOfLineVector.y += ballRadius;
    //                ballSpawnVector = firstBallOfLineVector;
    //            }
    //            else if (ballRow != 1)
    //            {
    //                ballSpawnVector.y -= ballRadius * 2;
    //            }
    //            else
    //            {
    //                firstBallOfLineVector = ballSpawnVector;
    //            }
    //            var ball = SpawnTriangleBall(ballSpawnVector, ballIndex);
    //            balls.Add(ball.BallGameObject, ball);
    //            ballIndex++;
    //        }
    //    }
    //    return balls;
    //}

    //private static BallData SpawnTriangleBall(Vector2 spawnPosition, int ballIndex)
    //{
    //    if (ballIndex > 15) ballIndex = new System.Random().Next(1, 15);
    //    var ballTypeColour = ballSpawnPattern[ballIndex - 1];

    //    GameObject ballGameObject = Instantiate(Resources.Load("Prefabs/ObjectBall"), spawnPosition, Quaternion.identity) as GameObject;

    //    if (ballGameObject == null) throw new InvalidOperationException("ballGameObject is null.");

    //    BallData ball = new BallData(ballTypeColour, ballGameObject);

    //    ballGameObject.tag = ballTypeColour.ToString();
    //    ballGameObject.name = ballTypeColour.ToString();
    //    switch (ballTypeColour)
    //    {
    //        case BallColour.Red:
    //            ballGameObject.GetComponent<SpriteRenderer>().color = Color.red;
    //            break;
    //        case BallColour.Yellow:
    //            ballGameObject.GetComponent<SpriteRenderer>().color = Color.yellow;
    //            break;
    //        case BallColour.Black:
    //            ballGameObject.GetComponent<SpriteRenderer>().color = Color.black;
    //            break;
    //        default:
    //            throw new InvalidOperationException($"Unexpected ball type: {ballTypeColour}");
    //    }
    //    return ball;
    //}

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

    public static GameObject SpawnSpecificColourBall(BallColour ballColour, BallSpawnLocations spawnPositionSelector)
    {
        Vector2 spawnPosition;
        switch (spawnPositionSelector)
        {
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

        GameObject ballGameObject = Instantiate(Resources.Load($"Prefabs/{ballColour}Ball"), spawnPosition, Quaternion.identity) as GameObject;

        if (ballGameObject == null) throw new InvalidOperationException("ballGameObject is null.");
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
