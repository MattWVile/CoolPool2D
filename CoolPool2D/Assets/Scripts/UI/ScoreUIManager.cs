using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreUIManager : MonoBehaviour
{
    public static ScoreUIManager Instance { get; private set; }

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

        // subscribe to score updates
        EventBus.Subscribe<ShotScoreTypeUpdatedEvent>(OnScoreUpdated);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<ShotScoreTypeUpdatedEvent>(OnScoreUpdated);
    }

    public void OnScoreUpdated(ShotScoreTypeUpdatedEvent shotScoreTypeUpdatedEvent)
    {
        AddScoreType(shotScoreTypeUpdatedEvent.ScoreType.ScoreTypeHeader);
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

        ScoreType scoreType = ScoreManager.Instance.currentScoreTypes.Find(scoreType => scoreType.ScoreTypeHeader == scoreTypeHeader);
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

    private void CreateNewScoreTypeElement(string scoreTypeHeader, ScoreType scoreType)
    {
        VisualElement scoreTypeVisualElement = shotTypeTemplate.Instantiate();
        scoreTypeVisualElement.Q<Label>("ScoreTypeHeading").text = scoreTypeHeader;
        scoreTypeVisualElement.Q<Label>("ScoreTypeMultValue").text = scoreType.ScoreTypeMultiplierAddition == 0 ? "" : scoreType.ScoreTypeMultiplierAddition.ToString();
        scoreTypeVisualElement.Q<Label>("ScoreTypeMultAdditionSymbol").text = scoreType.ScoreTypeMultiplierAddition == 0 ? "" : "+";
        scoreTypeVisualElement.Q<Label>("ScoreTypeMultAsterix").text = scoreType.ScoreTypeMultiplierAddition == 0 ? "" : "*";
        scoreTypeVisualElement.Q<Label>("ScoreTypeAmount").text = scoreType.NumberOfThisScoreType.ToString();
        scoreTypeVisualElement.Q<Label>("ScoreTypeScore").text = scoreType.ScoreTypePoints.ToString();
        scoreTypes.Add(scoreTypeVisualElement);

        VisualElement shotScoreBackground = root.Q<VisualElement>("ShotScoreTypes");
        if (shotScoreBackground != null)
        {
            shotScoreBackground.Add(scoreTypeVisualElement);
        }
        else
        {
            Debug.LogError("Container 'ShotScoreTypes' not found in the UI!");
        }
    }

    public void IncrementShotTypeAmount(VisualElement scoreType)
    {
        scoreType.Q<Label>("ScoreTypeAmount").text = (int.Parse(scoreType.Q<Label>("ScoreTypeAmount").text) + 1).ToString();
    }

    private void ClearScoreTypes()
    {
        foreach (VisualElement scoreType in scoreTypes)
        {
            scoreType.RemoveFromHierarchy();
        }
        scoreTypes.Clear();
    }
}
