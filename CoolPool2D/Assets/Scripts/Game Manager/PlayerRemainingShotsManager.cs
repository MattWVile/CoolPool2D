using UnityEngine;

public class PlayerRemainingShotsManager : MonoBehaviour
{
    private int maxAmountOfShots = 4;

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
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BallHasBeenShotEvent>(OnBallHasBeenShot);
        EventBus.Unsubscribe<NewGameStateEvent>(OnNewGameState);
    }

    private void OnBallHasBeenShot(BallHasBeenShotEvent @event)
    {
        ReduceAmountOfShotsByOne();
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

    private void ResetAmountOfShots()
    {
        amountOfShotsRemaining = maxAmountOfShots;
        UIManager.Instance?.UpdateRemainingShotsIcons(amountOfShotsRemaining, maxAmountOfShots);
    }
}
