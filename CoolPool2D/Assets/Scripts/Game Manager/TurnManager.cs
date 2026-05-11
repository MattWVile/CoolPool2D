using DamageNumbersPro;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public int currentTurn = 1;
    public List<GameObject> ballGameObjectsThatHaveHitRailsThisTurn;
    public bool shouldBallsAdvance;

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

    void Start()
    {
        EventBus.Subscribe<IScorableEvent>(OnScorableEvent);
        shouldBallsAdvance = true;
    }
    public void OnScorableEvent(IScorableEvent @event)
    {
        switch (@event)
        {
            case BallCollidedWithRailEvent:
                if (@event.BallData.ballVariant != BallVariant.Cue)
                {
                    ballGameObjectsThatHaveHitRailsThisTurn.Add(@event.BallData.gameObject);
                }
                break;
            case BallPocketedEvent:
                if (ballGameObjectsThatHaveHitRailsThisTurn.Contains(@event.BallData.gameObject))
                {
                    shouldBallsAdvance = false;
                    @event.BallData.ballMultiplier += 1;
                }
                break;
        }
    }

    public void PrepareForNextTurn()
    {
        try
        {
            var target = PoolWorld.Instance.GetNextTarget();
            if (target.gameObject && !GameManager.Instance.possibleTargets.Contains(target.gameObject))
                GameManager.Instance.possibleTargets.Add(target.gameObject);

        }
        catch (NullReferenceException)
        {
            //("No shootable found. placing one.");
            var cueBall = BallSpawner.SpawnCueBall(GameManager.Instance.amountOfCueBallsSpawned);
        }
        if (currentTurn % 3 == 0)
        {
            BallSpawner.SpawnAdvanceToBalkLineBall(BallSpawnLocations.RandomInFrontOfBalkLine);
        }
        BallSpawner.SpawnAdvanceToBalkLineBall(BallSpawnLocations.RandomInFrontOfBalkLine);
        ballGameObjectsThatHaveHitRailsThisTurn.Clear();
        shouldBallsAdvance = true;
        currentTurn++;
    }
}
