using Unity.VisualScripting;
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
            IncreaseAmountOfShotsByOne();
        }
    }

    private void OnNewGameState(NewGameStateEvent @event)
    {
        if (@event.NewGameState == GameState.GameStart)
        {
            ResetAmountOfShots();
        }
    }

    private void ReduceAmountOfShotsByOne()
    {
        amountOfShotsRemaining = Mathf.Max(0, amountOfShotsRemaining - 1);
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
}
