using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int currentScore = 0;
    public int highScore;

    public static ScoreManager Instance { get; private set; }

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
        highScore = DataManager.Instance.Data.ScoreData.HighScore;
    }

    private void OnScorableEvent(IScorableEvent @event)
    {
        if (@event.BallData.ballVariant != BallVariant.Cue)
        {
            switch (@event)
            {
                case BallCollidedWithRailEvent:
                    @event.BallData.ballMultiplier += 1;
                    break;
                case BallPocketedEvent:
                    IncreaseScore(Mathf.RoundToInt(@event.BallData.ballPoints * @event.BallData.ballMultiplier));
                    break;
            }   
        }
    }

    private void IncreaseScore(int amountToIncreaseBy)
    {
        currentScore += amountToIncreaseBy;
        UIManager.Instance?.UpdateCurrentScore(currentScore);
    }

    public int GetHighScore()
    {
        if (highScore < currentScore)
        {
            UpdateHighScore();
        }
        return highScore;
    }
    private void UpdateHighScore()
    {
        DataManager.Instance.Data.ScoreData = new ScoreData()
        {
            HighScore = currentScore
        };
        DataManager.Instance.SaveData();
        highScore = currentScore;
    }
}
