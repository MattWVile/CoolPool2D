using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreCardManager : MonoBehaviour
{
    public UIDocument uiDocument; // Assign this in the inspector
    private VisualElement root;
    public VisualTreeAsset shotTypeTemplate; // Assign the template .uxml here
    public VisualElement mostRecentShotAdded;

    public float shotScore;
    public float totalScore = 0f;

    public List<VisualElement> scoreTypes; // List to hold current score types

    void Start()
    {
        // Access the root element of the UI Document
        root = uiDocument.rootVisualElement;
        EventBus.Subscribe<BallCollidedWithRailEvent>(onBallCollidedWithRailEvent);
        scoreTypes = new List<VisualElement>();
        EventBus.Subscribe<NewGameStateEvent>((@event) =>
        {
            if (@event.NewGameState == GameState.CalculatePoints)
            {
                UpdateTotalScore();
                ClearShotScore();
            }
        });
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddScoreType("Testy Festyyyy", 1000f, 3, 1);
        }        
        if (Input.GetKeyDown(KeyCode.L))
        {
            UpdateTotalScore();
            ClearShotScore();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            foreach (VisualElement scoreType in scoreTypes)
            {
                if (scoreType.Q<Label>("ShotTypeHeading").text == "Testy Festyyyy")
                {
                    IncrimentShotTypeAmount(scoreType);
                    break;
                }
            }
        }
    }

    private void onBallCollidedWithRailEvent(BallCollidedWithRailEvent @event)
    {
        string shotTypeHeader = string.Empty;
        float shotTypeScore = 0f;
        bool foundScoreType = false;

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

        foreach (VisualElement scoreType in scoreTypes)
        {
            if (scoreType.Q<Label>("ShotTypeHeading").text == shotTypeHeader)
            {
                IncrimentShotTypeAmount(scoreType);
                foundScoreType = true;
                break;
            }
        }

        if (!foundScoreType)
        {
            AddScoreType(shotTypeHeader, shotTypeScore);
        }

        UpdateShotScore();
    }

    private VisualElement AddScoreType(string header, float score, int multiplier = 0, int amount = 1)
    {
        if (shotTypeTemplate == null)
        {
            Debug.LogError("ShotTypeTemplate is not assigned in the Inspector!");
            return null;
        }

        // Instantiate a new instance of the template
        VisualElement newShotType = shotTypeTemplate.Instantiate();

        // Update the "header" text
        newShotType.Q<Label>("ShotTypeHeading").text = header;

        // Update the multiplier, amount, and score
        newShotType.Q<Label>("ShotTypeMultValue").text = multiplier == 0 ? "" : multiplier.ToString();
        newShotType.Q<Label>("ShotTypeMultAdditionSymbol").text = multiplier == 0 ? "" : "+";
        newShotType.Q<Label>("ShotTypeMultAsterix").text = multiplier == 0 ? "" : "*";


        newShotType.Q<Label>("ShotTypeAmount").text = amount.ToString();
        newShotType.Q<Label>("ShotTypeScore").text = score.ToString();
        scoreTypes.Add(newShotType);
        // Add the populated template to the container in the main UI
        VisualElement shotScoreBackground = root.Q<VisualElement>("ShotScoreTypes");
        if (shotScoreBackground != null)
        {
            shotScoreBackground.Add(newShotType);
        }
        else
        {
            Debug.LogError("Container 'ShotScoreTypes' not found in the UI!");
        }

        return newShotType;
    }
    private void IncrimentShotTypeAmount(VisualElement scoreType)
    {
        scoreType.Q<Label>("ShotTypeAmount").text = (int.Parse(scoreType.Q<Label>("ShotTypeAmount").text) + 1).ToString();
    }

    private void UpdateShotScore()
    {
        float totalScore = 0f;
        float totalMultiplier = 1f;
        foreach (VisualElement scoreType in scoreTypes)
        {
            totalScore += float.Parse(scoreType.Q<Label>("ShotTypeScore").text) * int.Parse(scoreType.Q<Label>("ShotTypeAmount").text);
            string multValueText = scoreType.Q<Label>("ShotTypeMultValue").text;
            if (!string.IsNullOrEmpty(multValueText))
            {
                totalMultiplier += float.Parse(multValueText);
            }  
        }
        shotScore = totalScore * totalMultiplier;
        root.Q<Label>("ShotScoreScore").text = (totalScore * totalMultiplier).ToString();
    }
    public void UpdateTotalScore()
    {
        totalScore += shotScore;
        root.Q<Label>("TotalScore").text = totalScore.ToString();
    }
    public void ClearShotScore()
    {
        ClearScoreTypes();
        shotScore = 0f;
        root.Q<Label>("ShotScoreScore").text = "";
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
