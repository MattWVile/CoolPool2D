using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RewindBallToStartingPosition : MonoBehaviour
{
    public Vector2 StartingPosition;
    public struct PositionVelocityPair
    {
        public Vector2 Position;
        public Vector2 Velocity;

        public PositionVelocityPair(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
        }
    }

    public List<PositionVelocityPair> PositionAndNewVelocity = new();

    void Start()
    {
        EventBus.Subscribe<IScorableEvent>(OnScorableEvent);
        EventBus.Subscribe<BallStoppedEvent>(OnBallStopped);
        StartingPosition = gameObject.transform.position;
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<IScorableEvent>(OnScorableEvent);
        EventBus.Unsubscribe<BallStoppedEvent>(OnBallStopped);
    }
    public void OnScorableEvent(IScorableEvent @event)
    {
        if (@event is BallPocketedEvent) return;

        var selfBallData = gameObject.GetComponent<BallData>();

        if (@event is BallKissedEvent kissedEvent)
        {
            if (kissedEvent.BallData != selfBallData && kissedEvent.CollisionBallData != selfBallData)
                return;
        }
        else
        {
            if (@event.BallData != selfBallData) return;
        }

        HandleNewBallVectorAndVelocity();
    }

    public void HandleNewBallVectorAndVelocity()
    {
        var selfDeterministicBall = gameObject.GetComponent<DeterministicBall>();
        PositionAndNewVelocity.Add(
            new PositionVelocityPair(
                gameObject.transform.position,
                selfDeterministicBall.velocity
            )
        );
    }

    public void OnBallStopped(BallStoppedEvent ballStoppedEvent)
    {
        StartCoroutine(RewindCoroutine());
    }

    private IEnumerator RewindCoroutine()
    {
        var selfDeterministicBall = gameObject.GetComponent<DeterministicBall>();
        var rb = selfDeterministicBall; // For clarity

        // Go through the list in reverse
        for (int i = PositionAndNewVelocity.Count - 1; i >= 0; i--)
        {
            var pair = PositionAndNewVelocity[i];
            Vector2 targetPosition = pair.Position;
            Vector2 rewindVelocity = -pair.Velocity;

            // Only apply velocity once per rewind step
            rb.velocity = rewindVelocity;
            rb.initialVelocity = rewindVelocity;

            // Wait until the ball is close to the target position
            while (Vector2.Distance(rb.transform.position, targetPosition) > 0.05f)
            {
                // If the ball has stopped (velocity is very low), nudge it again
                if (rb.velocity.magnitude < 0.01f)
                {
                    rb.velocity = rewindVelocity;
                    rb.initialVelocity = rewindVelocity;
                }
                yield return new WaitForFixedUpdate();
            }

            // Snap to the target position and stop the ball
            rb.transform.position = targetPosition;
            rb.velocity = Vector2.zero;
            rb.initialVelocity = Vector2.zero;
            yield return null;
        }

        // Final snap to starting position
        rb.transform.position = StartingPosition;
        rb.velocity = Vector2.zero;
        rb.initialVelocity = Vector2.zero;
        PositionAndNewVelocity.Clear();
        StartingPosition = rb.transform.position;
    }
}