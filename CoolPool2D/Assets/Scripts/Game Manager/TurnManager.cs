using System;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public int currentTurn = 1;

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
        currentTurn ++;
    }
}
