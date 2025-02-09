using System;
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
        EventBus.Subscribe<IScorableEvent>(OnScorableEvent);
        Debug.Log("Subscribing to IScorableEvent");
    }

    public void OnScorableEvent(IScorableEvent @event)
    {
        string scoreTypeHeader = string.Empty;
        float scoreTypePoints = @event.ScoreTypePoints;
        bool isFoul = @event.IsFoul;

        switch (@event.Ball.BallGameObject.tag)
        {
            case "CueBall":
                scoreTypeHeader = @event is BallPocketedEvent ? "Cue Ball Pot" : "Cue Ball Rail Bounce";
                scoreTypePoints = 0f;
                isFoul = true;
                break;
            case "YellowBall":
                scoreTypeHeader = @event is BallPocketedEvent ? "Yellow Ball Pot" : "Yellow Ball Rail Bounce";
                break;
            case "RedBall":
                scoreTypeHeader = @event is BallPocketedEvent ? "Red Ball Pot" : "Red Ball Rail Bounce";
                break;
            case "BlackBall":
                scoreTypeHeader = @event is BallPocketedEvent ? "Black Ball Pot" : "Black Ball Rail Bounce";
                scoreTypePoints = 0f;
                isFoul = true;
                break;
            default:
                throw new InvalidOperationException($"Unexpected ball tag: {@event.Ball.BallGameObject.tag}");
        }

        AddOrUpdateScoreType(scoreTypeHeader, scoreTypePoints, isFoul);
    }

    private void AddOrUpdateScoreType(string scoreTypeHeader, float scoreTypePoints, bool isScoreTypeAFoul = false)
    {
        ScoreType scoreType = currentScoreTypes.Find(scoreType => scoreType.ScoreTypeName == scoreTypeHeader);
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
