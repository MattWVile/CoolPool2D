using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public float totalScore = 0f;
    public List<ScoreType> currentScoreTypes = new List<ScoreType>();

    public float scoreToBeat = 7500f;

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

    public void IncreaseScoreToBeat()
    {
               scoreToBeat *= 1.5f;
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
}
