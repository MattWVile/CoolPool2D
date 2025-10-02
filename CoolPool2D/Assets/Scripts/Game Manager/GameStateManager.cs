using UnityEngine;

/// <summary>
/// The game state can either be followed by listening to the NewGameStateEvent or by checking the CurrentGameState property.
/// This manager is responsible for managing the game state (and only the game state).
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentGameState { get; private set; } = GameState.GameStart;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// This method is used (by the GameManager) to submit the end of a state.
    /// Upon ending a state, an event is published that there is a new game state.
    /// </summary>
    /// <param name="gameState">The game state that has ended.</param>
    public void SubmitEndOfState(GameState gameState)
    {
        switch(gameState) {
            case GameState.Aiming:
                CurrentGameState = GameState.Shooting;
                break;
            case GameState.Shooting:
                CurrentGameState = GameState.CalculatePoints;
                break;
            case GameState.CalculatePoints:
                if(!GameManager.Instance.playerHasShotsRemaining)
                {
                    CurrentGameState = GameState.GameOver;
                }
                else
                {
                    CurrentGameState = GameState.PrepareNextTurn;
                }
                break;
            case GameState.PrepareNextTurn:
                CurrentGameState = GameState.Aiming;
                break;
            case GameState.GameStart:
                CurrentGameState = GameState.Aiming;
                break;
        }
        EventBus.Publish(new NewGameStateEvent { Sender = this, NewGameState = CurrentGameState });
    }

    public void SetGameState(GameState gameState)
    {
        CurrentGameState = gameState;
        EventBus.Publish(new NewGameStateEvent { Sender = this, NewGameState = CurrentGameState });
    }
}
