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

    public void UpdateShotScore(float newShotScoreFloat)
    {
        root.Q<Label>("ShotScoreScore").text = newShotScoreFloat.ToString();
    }
    public void ClearShotScore()
    {
        ClearScoreTypes();
        root.Q<Label>("ShotScoreScore").text = "";
    }

    public void AddScoreType(string shotTypeHeader)
    {
        if (shotTypeTemplate == null)
        {
            Debug.LogError("ShotTypeTemplate is not assigned in the Inspector!");
            return;
        }
        ShotType shot = ScoreManager.Instance.currentShotTypes.Find(shot => shot.ShotTypeName == shotTypeHeader);
        bool isMostRecent = true;
        if (shot != null)
        {
            foreach (VisualElement scoreType in UIManager.Instance.scoreTypes)
            {
                if (scoreType.Q<Label>("ShotTypeHeading").text == shotTypeHeader)
                {
                    UIManager.Instance.IncrementShotTypeAmount(scoreType);
                    isMostRecent = false;
                    break;
                }
            }

        }
        if(isMostRecent)
        {
            VisualElement newShotType = shotTypeTemplate.Instantiate();
            newShotType.Q<Label>("ShotTypeHeading").text = shotTypeHeader;
            newShotType.Q<Label>("ShotTypeMultValue").text = shot.ShotTypeMultiplierAddition == 0 ? "" : shot.ShotTypeMultiplierAddition.ToString();
            newShotType.Q<Label>("ShotTypeMultAdditionSymbol").text = shot.ShotTypeMultiplierAddition == 0 ? "" : "+";
            newShotType.Q<Label>("ShotTypeMultAsterix").text = shot.ShotTypeMultiplierAddition == 0 ? "" : "*";
            newShotType.Q<Label>("ShotTypeAmount").text = shot.NumberOfThisShotType.ToString();
            newShotType.Q<Label>("ShotTypeScore").text = shot.ShotTypePoints.ToString();
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
