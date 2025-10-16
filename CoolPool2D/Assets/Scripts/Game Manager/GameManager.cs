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

    public List<ScriptableArtifactBase> activeArtifacts;

    public int amountOfCueBallsSpawned = 0;

    // legacy score / aiming fields kept (but NO dictionary)
    public int lastShotScore;

    public ScoreCalculator scoreCalculator;

    private readonly ShotRecorder shotRecorder = new ShotRecorder();

    public BallScoringDataSnapshot lastPottedBall;

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
        activeArtifacts = new List<ScriptableArtifactBase>()
        {
            new HomingDevice(),
            new PinballMachine(),
            new PocketBallDuplicator()
        };
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
            StartCoroutine(WaitForBallsStoppedThenSubmitEndOfState());
        });

        EventBus.Subscribe<ScoringFinishedEvent>(HandleScoringFinishedEvent);

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
        BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);

        BallSpawner.SpawnSpecificColourBall(BallColour.Black, BallSpawnLocations.TriangleCenter);

        BallSpawner.SpawnSpecificColourBall(BallColour.Purple, BallSpawnLocations.Random);

        //BallSpawner.SpawnSpecificColourBall(BallColour.Blue, BallSpawnLocations.Random);

        //BallSpawner.SpawnSpecificColourBall(BallColour.Black, BallSpawnLocations.Random);

        //BallSpawner.SpawnSpecificColourBall(BallColour.Orange, BallSpawnLocations.Random);

        //BallSpawner.SpawnSpecificColourBall(BallColour.Orange, BallSpawnLocations.Random);

        //BallSpawner.SpawnSpecificColourBall(BallColour.Orange, BallSpawnLocations.Random);

        CaptureCurrentShotSnapshot();
        UIManager.Instance?.SetScoreToBeat(ScoreManager.Instance.scoreToBeat);
        gameStateManager.SubmitEndOfState(GameState.GameStart);
    }
    
    public void StartNextLevel()
    {
        ballGameObjects.ForEach(Destroy);
        ballGameObjects.Clear();
        deterministicBalls.Clear();
        amountOfCueBallsSpawned = 0;
        lastShotScore = 0;
        playerHasShotsRemaining = true;
        ScoreManager.Instance.IncreaseScoreToBeat();

        BallSpawner.SpawnNextRoundBalls(shotRecorder.GetLastSnapshot());

        UIManager.Instance?.SetScoreToBeat(ScoreManager.Instance.scoreToBeat);
        UIManager.Instance?.UpdateTotalScore(scoreCalculator.totalScore);

        gameStateManager.SubmitEndOfState(GameState.PrepareNextLevel);
    }

    private void HandleScoringFinishedEvent(ScoringFinishedEvent scoringFinishedEvent)
    {
        if (scoringFinishedEvent.TotalScore >= ScoreManager.Instance.scoreToBeat)
        {
            playerHasShotsRemaining = false;
            gameStateManager.SetGameState(GameState.PrepareNextLevel);
            UIManager.Instance?.EnableLevelCompleteScreen(scoringFinishedEvent.TotalScore, ScoreManager.Instance.scoreToBeat);
        }
        else
        {
            gameStateManager.SubmitEndOfState(GameState.CalculatePoints);
        }
    }

    private void HandlePrepareNextTurnState()
    {
        try
        {
            var target = PoolWorld.Instance.GetNextTarget();
            if(target.gameObject && !possibleTargets.Contains(target.gameObject))
                possibleTargets.Add(target.gameObject);

        }
        catch (NullReferenceException)
        {
            //("No shootable found. placing one.");
            var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
        }

        StartCoroutine(WaitThenEndState(.1f, GameState.PrepareNextTurn));
    }

    public void ResetGame()
    {
        ballGameObjects.ForEach(Destroy);
        ballGameObjects.Clear();
        possibleTargets.Clear();
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
        UIManager.Instance?.EnableGameOverScreen(scoreCalculator.totalScore, ScoreManager.Instance.scoreToBeat);
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
        possibleTargets.Clear();

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

    private void HandlePocketedBall(BallPocketedEvent ballPocketedEvent)
    {
        lastPottedBall = new BallScoringDataSnapshot(ballPocketedEvent.BallData);
        ballGameObjects.Remove(ballPocketedEvent.BallData.gameObject);
        deterministicBalls.Remove(ballPocketedEvent.BallData.gameObject.GetComponent<DeterministicBall>());
        if(ballPocketedEvent.BallData.ballColour == BallColour.Cue){
            possibleTargets.Remove(ballPocketedEvent.BallData.gameObject);
        }
        Destroy(ballPocketedEvent.BallData.gameObject);
    }

    private void HandleAimingState()
    {
        var targetGameObject = possibleTargets.FirstOrDefault();
        if (targetGameObject == null)
        {
            targetGameObject = PoolWorld.Instance.GetNextTarget().gameObject;
            possibleTargets.Add(targetGameObject);
        }
        cueMovement.Enable(targetGameObject);
    }

    private void HandleShootingState()
    {
        StartCoroutine(CheckIfAllBallsStopped(true));
        cueMovement?.RunDisableRoutine(cueMovement.Disable(0.05f));
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
            //Warning("GameManager.AddBallToLists your ballToAdd param is null");
            return;
        }

        ballGameObjects.Add(ballToAdd);
        deterministicBalls.Add(ballToAdd.GetComponent<DeterministicBall>());
    }

    private IEnumerator CheckIfAllBallsStopped(bool publishEvent)
    {
        yield return new WaitForSeconds(0.1f);
        while (!AllBallsStopped())
        {
            yield return new WaitForSeconds(0.5f);
        }
        if (publishEvent) EventBus.Publish(new BallStoppedEvent());
    }
    private IEnumerator WaitForBallsStoppedThenSubmitEndOfState()
    {
        yield return StartCoroutine(CheckIfAllBallsStopped(false));
        gameStateManager.SubmitEndOfState(GameState.Shooting);
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