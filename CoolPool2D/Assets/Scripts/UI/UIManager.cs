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

    public void UpdateTotalScore()
    {
        root.Q<Label>("TotalScore").text = ScoreManager.Instance.totalScore.ToString();
    }

    public void UpdateShotScore()
    {
        root.Q<Label>("ShotScoreScore").text = ScoreManager.Instance.shotScore.ToString();
    }
    public void ClearShotScore()
    {
        ClearScoreTypes();
        root.Q<Label>("ShotScoreScore").text = "";
    }

    public void AddScoreType(string header, float score, int multiplier = 0, int amount = 1)
    {
        if (shotTypeTemplate == null)
        {
            Debug.LogError("ShotTypeTemplate is not assigned in the Inspector!");
            return;
        }
        bool foundScoreType = false;
        if (UIManager.Instance.scoreTypes != null) {
            foreach (VisualElement scoreType in UIManager.Instance.scoreTypes)
            {
                if (scoreType.Q<Label>("ShotTypeHeading").text == header)
                {
                    UIManager.Instance.IncrementShotTypeAmount(scoreType);
                    foundScoreType = true;
                    break;
                }
            }
        }
        if (!foundScoreType)
        {
            VisualElement newShotType = shotTypeTemplate.Instantiate();
            newShotType.Q<Label>("ShotTypeHeading").text = header;
            newShotType.Q<Label>("ShotTypeMultValue").text = multiplier == 0 ? "" : multiplier.ToString();
            newShotType.Q<Label>("ShotTypeMultAdditionSymbol").text = multiplier == 0 ? "" : "+";
            newShotType.Q<Label>("ShotTypeMultAsterix").text = multiplier == 0 ? "" : "*";
            newShotType.Q<Label>("ShotTypeAmount").text = amount.ToString();
            newShotType.Q<Label>("ShotTypeScore").text = score.ToString();
            scoreTypes.Add(newShotType);

            VisualElement shotScoreBackground = root.Q<VisualElement>("ShotScoreTypes");
            if (shotScoreBackground != null)
            {
                shotScoreBackground.Add(newShotType);
            }
            else
            {
                Debug.LogError("Container 'ShotScoreTypes' not found in the UI!");
            }
        }
        UpdateShotScore();
    }
    public void IncrementShotTypeAmount(VisualElement scoreType)
    {
        scoreType.Q<Label>("ShotTypeAmount").text = (int.Parse(scoreType.Q<Label>("ShotTypeAmount").text) + 1).ToString();
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
