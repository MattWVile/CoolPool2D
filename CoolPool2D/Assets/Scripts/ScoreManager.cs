using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public float totalScore = 0f;
    public List<ShotType> currentShotTypes = new List<ShotType>();

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
        ShotType shotType = currentShotTypes.Find(shot => shot.ShotTypeName == shotTypeHeader);
        // if shotTypeHeader is not in the list of currentShotTypes, add it
        if (shotType == null)
        {
            shotType = new ShotType(shotTypeHeader, shotTypePoints);
            currentShotTypes.Add(shotType);
        }
        else
        {
            // if shotTypeHeader is in the list of currentShotTypes, increment the NumberOfThisShotType
            shotType.NumberOfThisShotType++;
        }

        UIManager.Instance.AddToShotScore(shotType.ShotTypePoints);
        UIManager.Instance.AddScoreType(shotTypeHeader);
    }

    private float calculateShotScore()
    {
        float shotScore = 0f;
        foreach (ShotType shot in currentShotTypes)
        {
            if (shot.IsShotFoul)
            {
            return 0f;
            }
            shotScore += shot.NumberOfThisShotType * shot.ShotTypePoints;
        }
        return shotScore;
    }
    public void CalculateTotalPoints()
    {
        totalScore += calculateShotScore();
        UIManager.Instance.UpdateTotalScore(totalScore);
        UIManager.Instance.ClearShotScore();
        currentShotTypes.Clear();
    }
}
