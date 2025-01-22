using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject cue;
    public List<GameObject> possibleTargets;
    public List<GameObject> balls;
    public List<Rigidbody2D> ballRbs;
    public GameStateManager gameStateManager;

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
        var cueball = GameObject.Find("CueBall");
        if (cueball != null)
        {
            cue.GetComponent<CueMovement>().Enable(cueball);
        }
        else
        {
            Debug.LogError("CueBall not found.");
        }

        LoadBalls();

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
    private void HandlePocketedBall(BallPocketedEvent @event)
    {
        balls.Remove(@event.Ball);
        ballRbs.Remove(@event.Ball.GetComponent<Rigidbody2D>());
        Destroy(@event.Ball);
        ScoreManager.Instance.OnBallPocketed(@event);
    }

    private void LoadBalls()
    {
        balls = GameObject.FindGameObjectsWithTag("CueBall").ToList();
        balls.AddRange(GameObject.FindGameObjectsWithTag("ObjectBall"));
        ballRbs = balls.Select(ball => ball.GetComponent<Rigidbody2D>()).ToList();
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
        ScoreManager.Instance.CalculatePoints();
        StartCoroutine(WaitThenEndState(1f, GameState.CalculatePoints));
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
            LoadBalls();
        }

        StartCoroutine(WaitThenEndState(1f, GameState.PrepareNextTurn));
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
