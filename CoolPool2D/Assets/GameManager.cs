using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject cue;
    public List<GameObject> possibleTargets;
    public List<GameObject> balls;
    public List<Rigidbody2D> ballRbs;
    public GameStateManager gameStateManager;

    void Start()
    {
        cue = GameObject.Find("Cue");
        var cueball = GameObject.Find("CueBall");
        cue.GetComponent<CueMovement>().Enable(cueball);

        LoadBalls();


        EventBus.Subscribe<BallPocketedEvent>((@event) => {
            balls.Remove(@event.Ball);
            ballRbs.Remove(@event.Ball.GetComponent<Rigidbody2D>());
            Destroy(@event.Ball);
        });


        //game state stuff
        EventBus.Subscribe<BallHasBeenShotEvent>((@event) => {
            gameStateManager.SubmitEndOfState(GameState.Aiming);
        });
        EventBus.Subscribe<BallStoppedEvent>((@event) => {
            gameStateManager.SubmitEndOfState(GameState.Shooting);
        });
        EventBus.Subscribe<NewGameStateEvent>((@event) =>
        {
            switch(@event.NewGameState) {
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

    private void LoadBalls() {

        balls = GameObject.FindGameObjectsWithTag("Ball").ToList();
        ballRbs = balls.Select(ball => ball.GetComponent<Rigidbody2D>()).ToList();
    }
    private void HandleAimingState() {
        Debug.Log("HandleAimingState");
        var target = possibleTargets.First();
        if (target == null)
            target = FindObjectOfType<Shootable>().gameObject;
        cue.GetComponent<CueMovement>().Enable(target);
    }

    private void HandleShootingState() {
        Debug.Log("HandleShootingState");
        StartCoroutine(CheckIfAllBallsStopped());
        StartCoroutine(cue.GetComponent<CueMovement>().Disable(0.2f));
    }
    private void HandleCalculatePointsState() {
        Debug.Log("Calculating points. For now this means nothing");
        StartCoroutine(WaitThenEndState(1f,GameState.CalculatePoints));
    }
    private void HandlePrepareNextTurnState() {
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

        StartCoroutine(WaitThenEndState(1f,GameState.PrepareNextTurn));
    }

    // delete later
    private IEnumerator WaitThenEndState(float seconds, GameState gameState) {
        yield return new WaitForSeconds(seconds);
        gameStateManager.SubmitEndOfState(gameState);
    }

    // coroutine to check if all balls are stopped, and if so, publish BallStoppedEvent
    private IEnumerator CheckIfAllBallsStopped() {
        yield return new WaitForSeconds(0.5f);
        while (!AllBallsStopped()) {
            yield return new WaitForSeconds(0.5f);
        }
        EventBus.Publish(new BallStoppedEvent());
    }

    private bool AllBallsStopped() {
        return ballRbs.All(rb => rb.velocity.magnitude < 0.1f);
    }
}
