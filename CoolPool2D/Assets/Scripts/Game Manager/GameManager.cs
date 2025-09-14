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
        }
    }

    private void Start()
    {
        cue = GameObject.Find("Cue");
        cueMovement = cue.GetComponent<CueMovement>();

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
        SpawnSpecificBallAndCueBall(BallColour.Orange);
        var specificBall = BallSpawner.SpawnSpecificBall(BallColour.Orange, BallSpawnLocations.Random);
        AddBallToLists(BallColour.Orange, specificBall);
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
        catch (System.NullReferenceException)
        {
            Debug.Log("No shootable found. placing one.");
            var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
            AddBallToLists(BallColour.White, cueBall);
        }

        StartCoroutine(WaitThenEndState(.1f, GameState.PrepareNextTurn));
    }

    public void ResetGame()
    {
        ballGameObjects.ForEach(Destroy);
        ballGameObjects.Clear();
        deterministicBalls.Clear();
        amountOfCueBallsSpawned = 0;
        // Update the game state
        gameStateManager.SetGameState(GameState.GameStart);
    }

    public void SpawnBallTriangleAndCueBall()
    {
        //ballDictionary = BallSpawner.SpawnBallsInTriangle();
        var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
        amountOfCueBallsSpawned++;
        ballGameObjects.Add(cueBall);

        deterministicBalls = ballGameObjects.Select(ball => ball.GetComponent<DeterministicBall>()).ToList();
    }

    public void SpawnSpecificBallAndCueBall(BallColour ballColour)
    {

        var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
        ballGameObjects.Add(cueBall);

        amountOfCueBallsSpawned++;

        var specificBall = BallSpawner.SpawnSpecificBall(ballColour, BallSpawnLocations.NextToLowCenterPocket);
        ballGameObjects.Add(specificBall);

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
        cueMovement.RunDisableRoutine(cueMovement.Disable(0.05f));
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

    private void AddBallToLists(BallColour ballColour, GameObject ballToAdd)
    {
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
}
