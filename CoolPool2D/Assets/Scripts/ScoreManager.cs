using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public float totalScore = 0f;
    public List<ScoreType> currentScoreTypes = new List<ScoreType>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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
    private void OnBallCollidedWithRailEvent(BallCollidedWithRailEvent @event)
    {
        string shotTypeHeader = string.Empty;
        float shotTypePoints = 0f;

        switch (@event.Ball.tag)
        {
            case "CueBall":
                shotTypeHeader = "Cue Ball Rail Bounce";
                shotTypePoints = 100f;
                break;
            case "ObjectBall":
                shotTypeHeader = "Object Ball Rail Bounce";
                shotTypePoints = 100f;
                break;
            default:
                shotTypeHeader = "Tag not in case statement";
                shotTypePoints = 100f;
                break;
        }
        AddOrUpdateShotType(shotTypeHeader, shotTypePoints);
    }
    private void AddOrUpdateShotType(string shotTypeHeader, float shotTypePoints)
    {
        ScoreType shotType = currentScoreTypes.Find(shot => shot.ScoreTypeName == shotTypeHeader);
        // if shotTypeHeader is not in the list of currentShotTypes, add it
        if (shotType == null)
        {
            shotType = new ScoreType(shotTypeHeader, shotTypePoints);
            currentScoreTypes.Add(shotType);
        }
        else
        {
            // if shotTypeHeader is in the list of currentShotTypes, increment the NumberOfThisShotType
            shotType.NumberOfThisScoreType++;
        }

        UIManager.Instance.AddToShotScore(shotType.ScoreTypePoints);
        UIManager.Instance.AddScoreType(shotTypeHeader);
    }

    private float calculateShotScore()
    {
        float shotScore = 0f;
        foreach (ScoreType shot in currentScoreTypes)
        {
            if (shot.IsScoreFoul)
            {
            return 0f;
            }
            shotScore += shot.NumberOfThisScoreType * shot.ScoreTypePoints;
        }
        return shotScore;
    }
    public void CalculateTotalPoints()
    {
        totalScore += calculateShotScore();
        UIManager.Instance.UpdateTotalScore(totalScore);
        UIManager.Instance.ClearShotScore();
        currentScoreTypes.Clear();
    }
}
