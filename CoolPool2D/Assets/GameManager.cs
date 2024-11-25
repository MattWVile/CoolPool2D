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

    void Start()
    {
        cue = GameObject.Find("Cue");
        var cueball = GameObject.Find("CueBall");
        cue.GetComponent<CueMovement>().Enable(cueball);

        balls = GameObject.FindGameObjectsWithTag("Ball").ToList();
        ballRbs = balls.Select(ball => ball.GetComponent<Rigidbody2D>()).ToList();

        EventBus.Subscribe<BallHasBeenShotEvent>((@event) => {
            StartCoroutine(CheckIfAllBallsStopped());
            StartCoroutine(cue.GetComponent<CueMovement>().Disable(0.2f));
        });
        EventBus.Subscribe<BallStoppedEvent>((@event) =>
        {
            var target = possibleTargets.First();
            if (target == null) target = FindObjectOfType<Shootable>().gameObject;
            cue.GetComponent<CueMovement>().Enable(target);
        });
        EventBus.Subscribe<BallPocketedEvent>((@event) => {
            balls.Remove(@event.Ball);
            ballRbs.Remove(@event.Ball.GetComponent<Rigidbody2D>());
            Destroy(@event.Ball);
        });

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
