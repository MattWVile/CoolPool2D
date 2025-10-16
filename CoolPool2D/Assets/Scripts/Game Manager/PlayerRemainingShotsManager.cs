using UnityEngine;

public class PlayerRemainingShotsManager : MonoBehaviour
{
    private int maxAmountOfShots = 3;

    public int amountOfShotsRemaining;

    private void Start()
    {
        amountOfShotsRemaining = maxAmountOfShots;
        ResetAmountOfShots();
    }

    private void OnEnable()
    {
        // subscribe with named methods so we can unsubscribe cleanly
        EventBus.Subscribe<BallHasBeenShotEvent>(OnBallHasBeenShot);
        EventBus.Subscribe<NewGameStateEvent>(OnNewGameState);
        EventBus.Subscribe<BallPocketedEvent>(OnBallHasBeenPocketed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BallHasBeenShotEvent>(OnBallHasBeenShot);
        EventBus.Unsubscribe<NewGameStateEvent>(OnNewGameState);
        EventBus.Unsubscribe<BallPocketedEvent>(OnBallHasBeenPocketed);
    }

    private void OnBallHasBeenShot(BallHasBeenShotEvent @event)
    {
        ReduceAmountOfShotsByOne();
    }

    private void OnBallHasBeenPocketed(BallPocketedEvent @event)
    {
        if (!@event.BallData.CompareTag("CueBall"))
        {
            if (amountOfShotsRemaining != 0 && GameManager.Instance.ballGameObjects.Count != 1)
            {
                IncreaseAmountOfShotsByOne();
            }
        }
    }

    private void OnNewGameState(NewGameStateEvent @event)
    {
        switch (@event.NewGameState)
        {
            case GameState.GameStart:
                ResetAmountOfShots();
                break;
            case GameState.PrepareNextLevel:
                ResetAmountOfShots();
                break;
            case GameState.PrepareNextTurn:
                HandlePrepareNextTurn();
                break;
        }
    }

    private void ReduceAmountOfShotsByOne()
    {
        amountOfShotsRemaining = Mathf.Max(0, amountOfShotsRemaining - 1);
        if (amountOfShotsRemaining == 0)
        {
            GameManager.Instance.playerHasShotsRemaining = false;   
        }
        UIManager.Instance?.UpdateRemainingShotsIcons(amountOfShotsRemaining, maxAmountOfShots);
    }

    private void IncreaseAmountOfShotsByOne()
    {
        amountOfShotsRemaining = Mathf.Max(0, amountOfShotsRemaining + 1);
        UIManager.Instance?.UpdateRemainingShotsIcons(amountOfShotsRemaining, maxAmountOfShots);
    }

    private void ResetAmountOfShots()
    {
        amountOfShotsRemaining = maxAmountOfShots;
        UIManager.Instance?.UpdateRemainingShotsIcons(amountOfShotsRemaining, maxAmountOfShots);
    }

    private void HandlePrepareNextTurn()
    {
        if (GameManager.Instance.ballGameObjects.Count <= 1 && amountOfShotsRemaining > 1)
        {
            if(GameManager.Instance.lastPottedBall.ballColour != BallColour.Cue)
            {
                BallSpawner.SpawnSpecificColourBall(GameManager.Instance.lastPottedBall.ballColour, BallSpawnLocations.TriangleCenter);
                ReduceAmountOfShotsByOne();
            }
        }
    }
}
