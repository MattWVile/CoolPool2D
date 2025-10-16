using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewindBallToStartingPosition : MonoBehaviour
{
    public bool hasRewoundThisShot = false;
    public Vector2 StartingPosition;

    private float snapThreshold = 0.1f;

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
        EventBus.Subscribe<NewGameStateEvent>(OnNewGameStateEvent);
        StartingPosition = gameObject.transform.position;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        EventBus.Unsubscribe<IScorableEvent>(OnScorableEvent);
        EventBus.Unsubscribe<BallStoppedEvent>(OnBallStopped);
        EventBus.Unsubscribe<NewGameStateEvent>(OnNewGameStateEvent);
    }

    public void OnNewGameStateEvent(NewGameStateEvent @event)
    {
        if (@event.NewGameState == GameState.Aiming)
        {
            hasRewoundThisShot = false;
            StopAllCoroutines();
        }
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
        if (!hasRewoundThisShot)
        {
            StartCoroutine(RewindCoroutine());
        }
        hasRewoundThisShot = true;
    }

    private IEnumerator RewindCoroutine()
    {
        DeterministicBall deterministicBall = gameObject.GetComponent<DeterministicBall>();
        int count = PositionAndNewVelocity.Count;
        if (count == 0)
            yield break;

        for (int i = count - 1; i >= 0; i--)
        {
            PositionVelocityPair newPositionAndVelocityPair = PositionAndNewVelocity[i];
            Vector2 targetPosition = newPositionAndVelocityPair.Position;
            Vector2 rewindVelocity = -newPositionAndVelocityPair.Velocity;

            deterministicBall.velocity = rewindVelocity;
            deterministicBall.initialVelocity = rewindVelocity;
            if (i >= count - 2) deterministicBall.velocity *= 2.5f;

            while (Vector2.Distance(deterministicBall.transform.position, targetPosition) > snapThreshold)
            {
                float currentDistance = Vector2.Distance(deterministicBall.transform.position, targetPosition);
                yield return new WaitForFixedUpdate();
            }
            yield return null;
        }
        PositionAndNewVelocity.Clear();
        StartingPosition = deterministicBall.transform.position;
    }
}
