using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager instance;
    public static ScoreManager Instance { get { return instance; } }

    public float shotScore;
    public float totalScore = 0f;
    //private List<Shot> pastShots = new List<Shot>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        EventBus.Subscribe<BallCollidedWithRailEvent>(OnBallCollidedWithRailEvent);
    }

    public void OnBallPocketed(BallPocketedEvent @event)
    {
        // Handle ball pocketed logic
    }

    //public void OnBallStopped(BallStoppedEvent @event)
    //{
    //    CalculatePoints();
    //    shotScore = 0f;
    //}

    private void OnBallCollidedWithRailEvent(BallCollidedWithRailEvent @event)
    {
        string shotTypeHeader = string.Empty;
        float shotTypeScore = 0f;

        switch (@event.Ball.tag)
        {
            case "CueBall":
                shotTypeHeader = "Cue Ball Rail Bounce";
                shotTypeScore = 100f;
                break;
            case "ObjectBall":
                shotTypeHeader = "Object Ball Rail Bounce";
                shotTypeScore = 100f;
                break;
            default:
                shotTypeHeader = "Tag not in case";
                shotTypeScore = 100f;
                break;
        }
        shotScore += shotTypeScore;
        UIManager.Instance.AddScoreType(shotTypeHeader, shotTypeScore);

    }

    public void CalculatePoints()
    {
        // Calculate points logic
        totalScore += shotScore;
        UIManager.Instance.UpdateTotalScore();
        shotScore = 0f;
    }
}
