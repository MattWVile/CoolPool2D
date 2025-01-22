using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public float shotScore;
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
        // if shotTypeHeader is not in the list of currentShotTypes, add it
        if (!currentShotTypes.Exists(shot => shot.ShotTypeName == shotTypeHeader))
        {
            AddShot(new ShotType(shotTypeHeader, shotTypeScore));
        }
        else
        {
            // if shotTypeHeader is in the list of currentShotTypes, increment the NumberOfThisShotType
            ShotType shot = currentShotTypes.Find(shot => shot.ShotTypeName == shotTypeHeader);
            shot.NumberOfThisShotType++;
        }

        UIManager.Instance.AddScoreType(shotTypeHeader);
    }

    public void CalculatePoints()
    {
        // Calculate points logic
        totalScore += shotScore;
        UIManager.Instance.UpdateTotalScore(totalScore);
        shotScore = 0f;
        UIManager.Instance.ClearShotScore();
    }
    public void AddShot(ShotType shot)
    {
        currentShotTypes.Add(shot);
        totalScore += shot.ShotTypePoints;
        UIManager.Instance.UpdateTotalScore(totalScore);
        UIManager.Instance.AddScoreType(shot.ShotTypeName);
    }

}
