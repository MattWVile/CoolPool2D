using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public UIDocument uiDocument; // Assign this in the inspector
    private VisualElement root;
    public VisualTreeAsset shotTypeTemplate; // Assign the template .uxml here
    public VisualElement mostRecentShotAdded;

    public List<VisualElement> scoreTypes; // List to hold current score types

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
        }
    }

    void Start()
    {
        root = uiDocument.rootVisualElement;
        scoreTypes = new List<VisualElement>();
    }

    public void UpdateTotalScore(float newTotalScoreFloat)
    {
        root.Q<Label>("TotalScore").text = newTotalScoreFloat.ToString();
    }

    public void AddToShotScore(float shotScoreToAdd)
    {
        string shotScoreText = root.Q<Label>("ShotScoreScore").text;
        if (float.TryParse(shotScoreText, out float currentShotScore))
        {
            float newShotScore = currentShotScore + shotScoreToAdd;
            root.Q<Label>("ShotScoreScore").text = newShotScore.ToString();
        }
        else
        {
            root.Q<Label>("ShotScoreScore").text = shotScoreToAdd.ToString();   
        }
    }

    public void ClearShotScore()
    {
        ClearScoreTypes();
        root.Q<Label>("ShotScoreScore").text = "";
    }

    public void AddScoreType(string scoreTypeHeader)
    {
        if (shotTypeTemplate == null)
        {
            Debug.LogError("ShotTypeTemplate is not assigned in the Inspector!");
            return;
        }

        ScoreType scoreType = ScoreManager.Instance.currentScoreTypes.Find(scoreType => scoreType.ScoreTypeName == scoreTypeHeader);
        if (scoreType == null)
        {
            Debug.LogError($"ScoreType with header {scoreTypeHeader} not found in ScoreManager!");
            return;
        }

        VisualElement existingShotType = scoreTypes.Find(scoreType => scoreType.Q<Label>("ScoreTypeHeading").text == scoreTypeHeader);
        if (existingShotType != null)
        {
            IncrementShotTypeAmount(existingShotType);
        }
        else
        {
            CreateNewScoreTypeElement(scoreTypeHeader, scoreType);
        }
    }

    private void CreateNewScoreTypeElement(string shotTypeHeader, ScoreType score)
    {
        VisualElement scoreType = shotTypeTemplate.Instantiate();

        scoreType.Q<Label>("ScoreTypeHeading").text = shotTypeHeader;
        if (score.IsScoreFoul)
        {
            scoreType.Q<Label>("ScoreTypeAmount").text = "";
            scoreType.Q<Label>("ScoreTypeMultiplicationSign").text = "FOUL!";
        }
        else
        {
            scoreType.Q<Label>("ScoreTypeAmount").text = score.NumberOfThisScoreType.ToString();
        }
        scoreType.Q<Label>("ScoreTypeScore").text = score.ScoreTypePoints == 0 ? "" : score.ScoreTypePoints.ToString();
        scoreType.Q<Label>("ScoreTypeMultValue").text = score.ScoreTypeMultiplierAddition == 0 ? "" : score.ScoreTypeMultiplierAddition.ToString();
        scoreType.Q<Label>("ScoreTypeMultAdditionSymbol").text = score.ScoreTypeMultiplierAddition == 0 ? "" : "+";
        scoreType.Q<Label>("ScoreTypeMultAsterix").text = score.ScoreTypeMultiplierAddition == 0 ? "" : "*";

        scoreTypes.Add(scoreType);

        VisualElement shotScoreBackground = root.Q<VisualElement>("ShotScoreTypes");
        if (shotScoreBackground != null)
        {
            shotScoreBackground.Add(scoreType);
        }
        else
        {
            Debug.LogError("Container 'ShotScoreTypes' not found in the UI!");
        }
    }

    public void IncrementShotTypeAmount(VisualElement scoreType)
    {
        if (scoreType.Q<Label>("ScoreTypeMultiplicationSign").text != "FOUL!")
        {
            scoreType.Q<Label>("ScoreTypeAmount").text = (int.Parse(scoreType.Q<Label>("ScoreTypeAmount").text) + 1).ToString();
        }
    }

    private void ClearScoreTypes()
    {
        foreach (VisualElement scoreType in scoreTypes)
        {
            scoreType.RemoveFromHierarchy();
        }
        scoreTypes.Clear();
    }

    //TODO: Make UI show total points with UI Builder
    // should have a button to restart to gameStart state
}
