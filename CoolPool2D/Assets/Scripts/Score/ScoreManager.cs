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
    }

    public void OnScorableEvent(IScorableEvent @event)
    {
        string scoreTypeHeader = string.Empty;
        float scoreTypePoints = @event.ScoreTypePoints;
        bool isFoul = @event.IsFoul;
        scoreTypeHeader = $"{@event.BallData.BallColour} ball";
        if (@event is BallKissedEvent)
        {
            scoreTypeHeader = @event.ScoreTypeHeader;
        }
        else
        {
            scoreTypeHeader += @event.ScoreTypeHeader;
        }
            AddOrUpdateScoreType(scoreTypeHeader, scoreTypePoints, isFoul);
    }

    private void AddOrUpdateScoreType(string scoreTypeHeader, float scoreTypePoints, bool isScoreTypeAFoul = false)
    {
        ScoreType scoreType = currentScoreTypes.Find(scoreType => scoreType.ScoreTypeHeader == scoreTypeHeader);
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
        if (scoreType.ScoreTypeHeader.Contains("kissed") && scoreType.NumberOfThisScoreType <= 1)
        {
            return;
        }

        PublishShotScoreTypeUpdatedEvent(scoreType);
    }
    private void PublishShotScoreTypeUpdatedEvent(ScoreType scoreType)
    {
        var scorePublishedEvent = new ShotScoreTypeUpdatedEvent
        {
            Sender = this,
            ScoreType = scoreType
        };

        EventBus.Publish(scorePublishedEvent);
    }


    // TODO be deleted and moved to score calculation
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
        ScoreUIManager.Instance.UpdateTotalScore(totalScore);
        ScoreUIManager.Instance.ClearShotScore();
        currentScoreTypes.Clear();
    }

}
