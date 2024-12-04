using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreCardManager : MonoBehaviour
{
    public UIDocument uiDocument; // Assign this in the inspector
    private VisualElement root;
    public VisualTreeAsset shotTypeTemplate; // Assign the template .uxml here
    public VisualElement mostRecentShotAdded;

    public List<VisualElement> scoreTypes; // List to hold current score types

    void Start()
    {
        // Access the root element of the UI Document
        root = uiDocument.rootVisualElement;
        EventBus.Subscribe<BallCollidedWithRailEvent>(onBallCollidedWithRailEvent);
        scoreTypes = new List<VisualElement>();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddScoreType("Testy Festyyyy", 1000f, 3, 1);
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
        newShotType.Q<Label>("ShotTypeMult").text = multiplier == 0 ? "" : $"+{multiplier}*";


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
}
