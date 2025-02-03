using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject cue;
    public List<GameObject> possibleTargets;
    public List<GameObject> balls;
    public List<Rigidbody2D> ballRbs;
    public GameStateManager gameStateManager;

    private Vector3 clothDimensions;
    private Vector3 clothCenter;
    private Vector3 firstBallOfLineVector;
    private float ballRadius;


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
        ballRadius = Resources.Load("Prefabs/ObjectBall", typeof(GameObject)).GetComponent<SpriteRenderer>().bounds.size.x / 2;
        cue = GameObject.Find("Cue");
        var cueball = GameObject.Find("CueBall");
        if (cueball != null)
        {
            cue.GetComponent<CueMovement>().Enable(cueball);
        }
        else
        {
            Debug.LogError("CueBall not found.");
        }

        SpawnBallsInTriangle();
        LoadBallsToList();

        EventBus.Subscribe<BallPocketedEvent>(HandlePocketedBall);

        EventBus.Subscribe<BallHasBeenShotEvent>((@event) =>
        {
            gameStateManager.SubmitEndOfState(GameState.Aiming);
        });

        EventBus.Subscribe<BallStoppedEvent>((@event) =>
        {
            gameStateManager.SubmitEndOfState(GameState.Shooting);
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
            }
        });
    }

    private void SpawnBallsInTriangle()
    {
        int ballIndex = 1;
        Bounds clothBounds = GameObject.Find("Cloth").GetComponent<SpriteRenderer>().bounds;
        clothDimensions = clothBounds.size;
        clothCenter = clothBounds.center;
        clothCenter.x += clothDimensions.x / 5;

        var ballSpawnVector = clothCenter;
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

    private void SpawnBall(Vector3 spawnPosition, int ballIndex)
    {
        List<string> ballPattern = new List<string> { "Y", "R", "Y", "Y", "B", "R", "R", "Y", "R", "Y", "Y", "R", "R", "Y", "R" };

        GameObject ball = Instantiate(Resources.Load("Prefabs/ObjectBall"), spawnPosition, Quaternion.identity) as GameObject;
        if (ball == null) throw new InvalidOperationException("Ball is null.");
        if (ballIndex > 15) ballIndex = new System.Random().Next(1, 15);

        var ballTypeString = ballPattern[ballIndex - 1];

        if (ballTypeString == "R")
        {
            ball.tag = "RedBall";
            ball.GetComponent<SpriteRenderer>().color = Color.red;
        }
        else if (ballTypeString == "Y")
        {
            ball.tag = "YellowBall";
            ball.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else if (ballTypeString == "B")
        {
            ball.tag = "BlackBall";
            ball.GetComponent<SpriteRenderer>().color = Color.black;
        }
        else
        {
            throw new InvalidOperationException($"Unexpected ball type: {ballTypeString}");
        }
    }

    private void LoadBallsToList()
    {
        balls = GameObject.FindGameObjectsWithTag("CueBall").ToList();
        balls.AddRange(GameObject.FindGameObjectsWithTag("RedBall"));
        balls.AddRange(GameObject.FindGameObjectsWithTag("YellowBall"));
        balls.AddRange(GameObject.FindGameObjectsWithTag("BlackBall"));
        ballRbs = balls.Select(ball => ball.GetComponent<Rigidbody2D>()).ToList();
    }
    private void HandlePocketedBall(BallPocketedEvent @event)
    {
        balls.Remove(@event.Ball);
        ballRbs.Remove(@event.Ball.GetComponent<Rigidbody2D>());
        Destroy(@event.Ball);
        ScoreManager.Instance.OnBallPocketed(@event);
    }

    private void HandleAimingState()
    {
        Debug.Log("HandleAimingState");
        var target = possibleTargets.First();
        if (target == null)
            target = FindObjectOfType<Shootable>().gameObject;
        cue.GetComponent<CueMovement>().Enable(target);
    }

    private void HandleShootingState()
    {
        Debug.Log("HandleShootingState");
        StartCoroutine(CheckIfAllBallsStopped());
        StartCoroutine(cue.GetComponent<CueMovement>().Disable(0.2f));
    }

    private void HandleCalculatePointsState()
    {
        Debug.Log("Calculating points.");
        ScoreManager.Instance.CalculateTotalPoints();
        StartCoroutine(WaitThenEndState(.1f, GameState.CalculatePoints));
    }

    private void HandlePrepareNextTurnState()
    {
        Debug.Log("Preparing next turn.");
        try
        {
            var target = FindObjectOfType<Shootable>().gameObject;
        }
        catch (System.NullReferenceException)
        {
            Debug.Log("No shootable found. placing one.");
            Instantiate(Resources.Load<GameObject>("Prefabs/CueBall"));
            LoadBallsToList();
        }

        StartCoroutine(WaitThenEndState(.1f, GameState.PrepareNextTurn));
    }

    private IEnumerator WaitThenEndState(float seconds, GameState gameState)
    {
        yield return new WaitForSeconds(seconds);
        gameStateManager.SubmitEndOfState(gameState);
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
}
