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
        string scoreTypeHeader = string.Empty;
        float scoreTypePoints = 0f;
        bool isFoul = false;

        switch (@event.Ball.tag)
        {
            case "CueBall":
                scoreTypeHeader = "Cue Ball Pot";
                isFoul = true;
                break;
            case "ObjectBall":
                scoreTypeHeader = "Object Ball Pot";
                scoreTypePoints = 500f;
                break;
            default:
                scoreTypeHeader = "Tag not in case statement";
                break;
        }
        AddOrUpdateScoreType(scoreTypeHeader, scoreTypePoints, isFoul);
    }
    private void OnBallCollidedWithRailEvent(BallCollidedWithRailEvent @event)
    {
        string scoreTypeHeader = string.Empty;
        float scoreTypePoints = 0f;

        switch (@event.Ball.tag)
        {
            case "CueBall":
                scoreTypeHeader = "Cue Ball Rail Bounce";
                scoreTypePoints = 100f;
                break;
            case "ObjectBall":
                scoreTypeHeader = "Object Ball Rail Bounce";
                scoreTypePoints = 100f;
                break;
            default:
                scoreTypeHeader = "Tag not in case statement";
                break;
        }
        AddOrUpdateScoreType(scoreTypeHeader, scoreTypePoints);
    }
    private void AddOrUpdateScoreType(string scoreTypeHeader, float scoreTypePoints, bool isScoreTypeAFoul = false)
    {
        ScoreType scoreType = currentScoreTypes.Find(scoreType => scoreType.ScoreTypeName == scoreTypeHeader);
        // if scoreTypeHeader is not in the list of currentScoreTypes, add it
        if (scoreType == null)
        {
            scoreType = new ScoreType(scoreTypeHeader, scoreTypePoints, isScoreTypeAFoul);
            currentScoreTypes.Add(scoreType);
        }
        else if (isScoreTypeAFoul)
        {
            if (!scoreType.IsScoreFoul)
            {
                scoreType.IsScoreFoul = true;
            }
        }
        else
        {
            // if scoreTypeHeader is in the list of currentScoreTypes, increment the NumberOfThisScoreType
            scoreType.NumberOfThisScoreType++;
        }

        UIManager.Instance.AddToShotScore(scoreType.ScoreTypePoints);
        UIManager.Instance.AddScoreType(scoreTypeHeader);
    }

    private float calculateShotScore()
    {
        float shotScore = 0f;
        foreach (ScoreType scoreType in currentScoreTypes)
        {
            if (scoreType.IsScoreFoul)
            {
            return 0f;
            }
            shotScore += scoreType.NumberOfThisScoreType * scoreType.ScoreTypePoints;
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
