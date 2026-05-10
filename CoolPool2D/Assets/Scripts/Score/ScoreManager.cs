using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public float currentScore = 0f;

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
                    IncreaseScore(@event.BallData.ballPoints * @event.BallData.ballMultiplier);
                    break;
            }
        }
    }
    private void IncreaseScore(float amountToIncreaseBy)
    {
        currentScore += amountToIncreaseBy;
        UIManager.Instance?.UpdateCurrentScore(currentScore);
    }
}
