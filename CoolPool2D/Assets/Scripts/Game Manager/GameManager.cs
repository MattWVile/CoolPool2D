using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject cue;
    public CueMovement cueMovement;
    public List<GameObject> possibleTargets;
    public List<GameObject> ballGameObjects;
    public List<DeterministicBall> deterministicBalls;
    public GameStateManager gameStateManager;

    public int amountOfCueBallsSpawned = 0;

    // legacy score / aiming fields kept (but NO dictionary)
    public int lastShotScore;

    public ScoreCalculator scoreCalculator;

    private readonly ShotRecorder shotRecorder = new ShotRecorder();

    public BallData lastPottedBall;

    public bool playerHasShotsRemaining = true;
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
        cueMovement = cue.GetComponent<CueMovement>();
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
                case GameState.GameOver:
                    HandleGameOverState();
                    break;
            }
        });

        gameStateManager.SetGameState(GameState.GameStart);
    }

    public void StartGame()
    {
        var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);

        var specificBall = BallSpawner.SpawnSpecificColourBall(BallColour.Black, BallSpawnLocations.TriangleCenter);

        //var specificBall = BallSpawner.SpawnSpecificBall(BallColour.Orange, BallSpawnLocations.Random);

        //var specificBall2 = BallSpawner.SpawnSpecificBall(BallColour.Orange, BallSpawnLocations.Random);

        //var specificBall3 = BallSpawner.SpawnSpecificBall(BallColour.Orange, BallSpawnLocations.Random);

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
        }

        StartCoroutine(WaitThenEndState(.1f, GameState.PrepareNextTurn));
    }

    public void ResetGame()
    {
        ballGameObjects.ForEach(Destroy);
        ballGameObjects.Clear();
        deterministicBalls.Clear();
        amountOfCueBallsSpawned = 0;
        lastShotScore = 0;
        scoreCalculator.totalScore = 0;
        playerHasShotsRemaining = true;
        UIManager.Instance?.UpdateTotalScore(scoreCalculator.totalScore);
        gameStateManager.SetGameState(GameState.GameStart);
    }
    public void HandleGameOverState()
    {
        ballGameObjects.ForEach(Destroy);
        ballGameObjects.Clear();
        deterministicBalls.Clear();
        UIManager.Instance?.EnableGameOverScreen(scoreCalculator.totalScore);
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

        BallSpawner.SpawnLastShotBalls(shotRecorder.GetLastSnapshot());

        if (scoreCalculator != null)
        {
            scoreCalculator.totalScore -= lastShotScore;
            lastShotScore = 0;
            UIManager.Instance?.UpdateTotalScore(scoreCalculator.totalScore);
        }
        gameStateManager.SetGameState(GameState.Aiming);
    }

    public void SpawnBallTriangleAndCueBall()
    {
        var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
        
        deterministicBalls = ballGameObjects.Select(ball => ball.GetComponent<DeterministicBall>()).ToList();
    }

    private void HandlePocketedBall(BallPocketedEvent @event)
    {
        lastPottedBall = @event.BallData;
        ballGameObjects.Remove(@event.BallData.gameObject);
        deterministicBalls.Remove(@event.BallData.gameObject.GetComponent<DeterministicBall>());
        Destroy(@event.BallData.gameObject);
    }

    private void HandleAimingState()
    {
        Debug.Log("HandleAimingState");
        var targetGameObject = possibleTargets.First();
        if (targetGameObject == null)
        {
            targetGameObject = PoolWorld.Instance.GetNextTarget().gameObject;
        }
        possibleTargets.Add(targetGameObject);
        cueMovement.Enable(targetGameObject);
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

    public void AddBallToLists(GameObject ballToAdd)
    {
        if (ballToAdd == null)
        {
            Debug.LogWarning("GameManager.AddBallToLists your ballToAdd param is null");
            return;
        }

        ballGameObjects.Add(ballToAdd);
        deterministicBalls.Add(ballToAdd.GetComponent<DeterministicBall>());
    }

    private IEnumerator CheckIfAllBallsStopped()
    {
        yield return new WaitForSeconds(0.5f);
        while (!AllBallsStopped())
        {
            yield return new WaitForSeconds(0.5f);
        }
        EventBus.Publish(new BallStoppedEvent());
    }

    public bool AllBallsStopped()
    {
        return deterministicBalls.All(rb => rb.velocity.magnitude < 0.1f);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}