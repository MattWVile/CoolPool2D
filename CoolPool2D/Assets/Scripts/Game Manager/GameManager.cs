using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class BallSnapshot
{
    public int InstanceId;
    public BallColour Colour;
    public Vector2 Position;
    public Vector2 Velocity;
    public bool Active;
}

public class ShotRecorder
{
    private readonly List<BallSnapshot> _lastShotSnapshot = new List<BallSnapshot>();

    public void SaveSnapshot(IEnumerable<GameObject> balls)
    {
        _lastShotSnapshot.Clear();
        if (balls == null) return;

        foreach (var go in balls)
        {
            if (go == null) continue;
            var data = go.GetComponent<BallData>();
            if (data == null) continue;
            var det = go.GetComponent<DeterministicBall>();

            var snap = new BallSnapshot
            {
                InstanceId = go.GetInstanceID(),
                Colour = data.BallColour,
                Position = (Vector2)go.transform.position,
                Velocity = det != null ? det.velocity : Vector2.zero,
                Active = det != null ? det.active : go.activeInHierarchy
            };
            _lastShotSnapshot.Add(snap);
        }
    }

    public IReadOnlyList<BallSnapshot> GetLastSnapshot() => _lastShotSnapshot.AsReadOnly();

    // Restore by setting existing objects (match by instance id). Useful if you never destroyed originals.
    public void RestoreSnapshotToExistingObjects()
    {
        if (_lastShotSnapshot == null || _lastShotSnapshot.Count == 0) return;

        var map = _lastShotSnapshot.ToDictionary(s => s.InstanceId);
        foreach (var det in UnityEngine.Object.FindObjectsOfType<DeterministicBall>())
        {
            if (det == null) continue;
            var go = det.gameObject;
            if (map.TryGetValue(go.GetInstanceID(), out var snap))
            {
                go.transform.position = snap.Position;
                det.velocity = snap.Velocity;
                det.active = snap.Active;
            }
        }
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject cue;
    public CueMovement cueMovement;
    public List<GameObject> possibleTargets = new List<GameObject>();
    public List<GameObject> ballGameObjects = new List<GameObject>();
    public List<DeterministicBall> deterministicBalls = new List<DeterministicBall>();
    public GameStateManager gameStateManager;

    public int amountOfCueBallsSpawned = 0;

    // legacy score / aiming fields kept (but NO dictionary)
    public int lastShotScore;

    public ScoreCalculator scoreCalculator;

    private readonly ShotRecorder shotRecorder = new ShotRecorder();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        cue = GameObject.Find("Cue");
        cueMovement = cue != null ? cue.GetComponent<CueMovement>() : null;
        var scoreManagerObj = GameObject.Find("ScoreManager");
        if (scoreManagerObj != null)
            scoreCalculator = scoreManagerObj.GetComponent<ScoreCalculator>();

        EventBus.Subscribe<BallPocketedEvent>(HandlePocketedBall);

        EventBus.Subscribe<BallHasBeenShotEvent>((@event) =>
        {
            gameStateManager.SubmitEndOfState(GameState.Aiming);
        });

        EventBus.Subscribe<BallStoppedEvent>((@event) =>
        {
            gameStateManager.SubmitEndOfState(GameState.Shooting);
        });

        EventBus.Subscribe<ScoringFinishedEvent>((@event) =>
        {
            gameStateManager.SubmitEndOfState(GameState.CalculatePoints);
        });

        EventBus.Subscribe<NewGameStateEvent>((@event) =>
        {
            switch (@event.NewGameState)
            {
                case GameState.Aiming:
                    HandleAimingState();
                    break;
                case GameState.Shooting:
                    HandleShootingState();
                    break;
                case GameState.CalculatePoints:
                    HandleCalculatePointsState();
                    break;
                case GameState.PrepareNextTurn:
                    HandlePrepareNextTurnState();
                    break;
                case GameState.GameStart:
                    StartGame();
                    break;
            }
        });

        gameStateManager.SetGameState(GameState.GameStart);
    }

    public void StartGame()
    {
        SpawnSpecificBallAndCueBall(BallColour.Black);

        var specificBall = BallSpawner.SpawnSpecificBall(BallColour.Orange, BallSpawnLocations.Random);
        AddBallToLists(specificBall);

        var specificBall2 = BallSpawner.SpawnSpecificBall(BallColour.Orange, BallSpawnLocations.Random);
        AddBallToLists(specificBall2);

        var specificBall3 = BallSpawner.SpawnSpecificBall(BallColour.Orange, BallSpawnLocations.Random);
        AddBallToLists(specificBall3);

        CaptureCurrentShotSnapshot();
        gameStateManager.SubmitEndOfState(GameState.GameStart);
    }

    private void HandlePrepareNextTurnState()
    {
        Debug.Log("Preparing next turn.");
        try
        {
            var target = PoolWorld.Instance.GetNextTarget();
            possibleTargets.Add(target.gameObject);
        }
        catch (NullReferenceException)
        {
            Debug.Log("No shootable found. placing one.");
            var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
            AddBallToLists(cueBall);
        }

        StartCoroutine(WaitThenEndState(.1f, GameState.PrepareNextTurn));
    }

    public void ResetGame()
    {
        ballGameObjects.ForEach(Destroy);
        ballGameObjects.Clear();
        deterministicBalls.Clear();
        amountOfCueBallsSpawned = 0;
        gameStateManager.SetGameState(GameState.GameStart);
    }

    public void CaptureCurrentShotSnapshot()
    {
        shotRecorder.SaveSnapshot(ballGameObjects);
    }

    public void RetryLastShot()
    {
        ballGameObjects.ForEach(Destroy);
        ballGameObjects.Clear();
        deterministicBalls.Clear();

        var snaps = shotRecorder.GetLastSnapshot();
        foreach (var snap in snaps)
        {
            var ballGameObject = SpawnBallAtPosition(snap.Colour, snap.Position);
            if (ballGameObject == null)
            {
                Debug.LogError($"Failed to spawn ball for colour {snap.Colour} at {snap.Position}");
                continue;
            }
            var det = ballGameObject.GetComponent<DeterministicBall>();
            if (det != null)
            {
                det.velocity = snap.Velocity;
                det.active = snap.Active;
            }

            AddBallToLists(ballGameObject);
        }

        if (scoreCalculator != null)
        {
            scoreCalculator.totalScore -= lastShotScore;
            lastShotScore = 0;
            ScoreUIManager.Instance?.UpdateTotalScore(scoreCalculator.totalScore);
        }
        gameStateManager.SetGameState(GameState.Aiming);
    }

    public void SpawnBallTriangleAndCueBall()
    {
        var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
        AddBallToLists(cueBall);
        deterministicBalls = ballGameObjects.Select(ball => ball.GetComponent<DeterministicBall>()).ToList();
    }

    public void SpawnSpecificBallAndCueBall(BallColour ballColour)
    {
        var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
        AddBallToLists(cueBall);

        var specificBall = BallSpawner.SpawnSpecificBall(ballColour, BallSpawnLocations.TriangleCenter);
        AddBallToLists(specificBall);

        deterministicBalls = ballGameObjects.Select(ball => ball.GetComponent<DeterministicBall>()).ToList();
    }

    private void HandlePocketedBall(BallPocketedEvent @event)
    {
        ballGameObjects.Remove(@event.BallData.gameObject);
        deterministicBalls.Remove(@event.BallData.gameObject.GetComponent<DeterministicBall>());
        Destroy(@event.BallData.gameObject);
    }

    private void HandleAimingState()
    {
        Debug.Log("HandleAimingState");
        GameObject targetGameObject = null;
        if (possibleTargets != null && possibleTargets.Count > 0)
            targetGameObject = possibleTargets.FirstOrDefault();
        if (targetGameObject == null)
        {
            var next = PoolWorld.Instance.GetNextTarget();
            if (next != null) targetGameObject = next.gameObject;
        }

        if (targetGameObject != null)
        {
            possibleTargets.Add(targetGameObject);
            cueMovement?.Enable(targetGameObject);
        }
    }

    private void HandleShootingState()
    {
        Debug.Log("HandleShootingState");
        StartCoroutine(CheckIfAllBallsStopped());
        cueMovement?.RunDisableRoutine(cueMovement.Disable(0.05f));
    }

    private void HandleCalculatePointsState()
    {
        Debug.Log("Calculating points.");
    }

    private IEnumerator WaitThenEndState(float seconds, GameState gameState)
    {
        yield return new WaitForSeconds(seconds);
        gameStateManager.SubmitEndOfState(gameState);
    }

    private void AddBallToLists(GameObject ballToAdd)
    {
        if (ballToAdd == null) return;
        ballGameObjects.Add(ballToAdd);
        var det = ballToAdd.GetComponent<DeterministicBall>();
        if (det != null)
            deterministicBalls.Add(det);
    }

    private IEnumerator CheckIfAllBallsStopped()
    {
        yield return new WaitForSeconds(0.5f);
        while (!AllBallsStopped())
            yield return new WaitForSeconds(0.5f);
        EventBus.Publish(new BallStoppedEvent());
    }

    public bool AllBallsStopped()
    {
        return deterministicBalls.All(rb => rb != null && rb.velocity.magnitude < 0.1f);
    }

    private GameObject SpawnBallAtPosition(BallColour colour, Vector2 position)
    {
        var spawnerType = typeof(BallSpawner);
        var methods = spawnerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        MethodInfo found = null;
        foreach (var m in methods)
        {
            var ps = m.GetParameters();
            if (ps.Length == 2 && ps[0].ParameterType == typeof(BallColour) && ps[1].ParameterType == typeof(Vector2))
            {
                found = m;
                break;
            }
        }

        if (found != null)
        {
            var result = found.Invoke(null, new object[] { colour, position });
            return result as GameObject;
        }

        // Fallback: look for SpawnSpecificBall(BallColour, BallSpawnLocations)
        var fallback = methods.FirstOrDefault(m =>
        {
            var ps = m.GetParameters();
            return ps.Length == 2 && ps[0].ParameterType == typeof(BallColour) &&
                   ps[1].ParameterType.IsEnum && ps[1].ParameterType.Name == nameof(BallSpawnLocations);
        });

        if (fallback != null)
        {
            // spawn at a deterministic default (TriangleCenter) then move to exact position
            var spawned = fallback.Invoke(null, new object[] { colour, Enum.Parse(fallback.GetParameters()[1].ParameterType, BallSpawnLocations.TriangleCenter.ToString()) }) as GameObject;
            if (spawned != null)
            {
                spawned.transform.position = position;
            }
            return spawned;
        }

        Debug.LogError("No suitable BallSpawner spawn method found. Please add a SpawnSpecificBall(BallColour, Vector2) method or adjust this code.");
        return null;
    }
}
