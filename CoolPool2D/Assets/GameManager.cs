using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public PlayerBallColor playerColor = PlayerBallColor.None;
    public static GameManager Instance { get; private set; }

    public GameObject cue;
    public List<GameObject> possibleTargets;
    public List<GameObject> ballGameObjects;
    public List<Rigidbody2D> ballRbs;
    public Dictionary<GameObject, Ball> ballDictionary = new Dictionary<GameObject, Ball>();
    public GameStateManager gameStateManager;

    public float amountOfRedBalls = 1;
    public float amountOfYellowBalls = 1;

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

        EventBus.Subscribe<BallPocketedEvent>(HandlePocketedBall);

        EventBus.Subscribe<BallHasBeenShotEvent>((@event) =>
        {
            gameStateManager.SubmitEndOfState(GameState.Aiming);
        });

        EventBus.Subscribe<BallStoppedEvent>((@event) =>
        {

            if (gameStateManager.CurrentGameState != GameState.GameOver)
            {
                gameStateManager.SubmitEndOfState(GameState.Shooting);
            }
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
        SpawnBallTriangleAndCueBall();
        gameStateManager.SubmitEndOfState(GameState.GameStart);
    }

    public void SpawnBallTriangleAndCueBall()
    {
        ballDictionary = BallSpawner.SpawnBallsInTriangle();
        var cueBall = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned);
        amountOfCueBallsSpawned++;
        ballDictionary.Add(cueBall.BallGameObject, cueBall);

        ballGameObjects = ballDictionary.Keys.ToList();
        ballRbs = ballGameObjects.Select(ball => ball.GetComponent<Rigidbody2D>()).ToList();
    }

    private void HandlePocketedBall(BallPocketedEvent @event)
    {
        ballGameObjects.Remove(@event.Ball.BallGameObject);
        ballRbs.Remove(@event.Ball.BallGameObject.GetComponent<Rigidbody2D>());
        ballDictionary.Remove(@event.Ball.BallGameObject);
        UpdateBallCounts(@event.Ball);
        Destroy(@event.Ball.BallGameObject);
        @event.Ball = null;
    }

    private void UpdateBallCounts(Ball ball)
    {
        if (ball.BallGameObject.CompareTag("RedBall") && amountOfRedBalls > 0)
        {
            amountOfRedBalls--;
        }
        else if (ball.BallGameObject.CompareTag("YellowBall") && amountOfYellowBalls > 0)
        {
            amountOfYellowBalls--;
        }

        if (amountOfRedBalls == 0 || amountOfYellowBalls == 0)
        {
            playerColor = PlayerBallColor.Black;
            CheckForGameOver(ball);
        }
    }

    private void CheckForGameOver(Ball ball)
    {
        if (ball.BallGameObject.CompareTag("BlackBall") && playerColor == PlayerBallColor.Black)
        {
            gameStateManager.SetGameState(GameState.GameOver);
        }
    }

    private void HandleAimingState()
    {
        var target = possibleTargets.First();
        if (target == null)
            target = FindObjectOfType<Shootable>().gameObject;
        possibleTargets.Add(target);
        cue.GetComponent<CueMovement>().Enable(target);
    }

    private void HandleShootingState()
    {
        StartCoroutine(CheckIfAllBallsStopped());
        StartCoroutine(cue.GetComponent<CueMovement>().Disable(0.2f));
    }

    private void HandleCalculatePointsState()
    {
        ScoreManager.Instance.CalculateTotalPoints();
        StartCoroutine(WaitThenEndState(.5f, GameState.CalculatePoints));
    }

    private void HandlePrepareNextTurnState()
    {
        try
        {
            var target = FindObjectOfType<Shootable>().gameObject;
            possibleTargets.Add(target);
        }
        catch (System.NullReferenceException)
        {
            Debug.Log("No shootable found. placing one.");
            var newCueBallGameObject = BallSpawner.SpawnCueBall(amountOfCueBallsSpawned).BallGameObject;
            AddBallToLists(newCueBallGameObject);
        }

        StartCoroutine(WaitThenEndState(.1f, GameState.PrepareNextTurn));
    }

    private IEnumerator WaitThenEndState(float seconds, GameState gameState)
    {
        yield return new WaitForSeconds(seconds);
        gameStateManager.SubmitEndOfState(gameState);
    }

    private void AddBallToLists(GameObject ballToAdd)
    {
        ballGameObjects.Add(ballToAdd);
        ballRbs.Add(ballToAdd.GetComponent<Rigidbody2D>());
        var ball = new Ball(ballToAdd.name, ballToAdd);
        ballDictionary.Add(ballToAdd, ball);
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

    private bool AllBallsStopped()
    {
        return ballRbs.All(rb => rb.velocity.magnitude < 0.1f);
    }
    private void DespawnAllBalls()
    {
        foreach (var ballGameObject in ballGameObjects)
        {
            Destroy(ballGameObject);
        }
        ballGameObjects.Clear();
        ballRbs.Clear();
        ballDictionary.Clear();
    }
    private void ShowTotalPoints()
    {
        // Assuming you have a UIManager to handle UI updates
        Debug.Log("Total score: " + ScoreManager.Instance.totalScore);
    }
    private void HandleGameOverState()
    {
        ScoreManager.Instance.CalculateTotalPoints();
        ShowTotalPoints();
        DespawnAllBalls();
    }

}
