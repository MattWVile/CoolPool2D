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
    }
    private void onBallCollidedWithRailEvent(BallCollidedWithRailEvent @event)
    {
        if (@event.Ball.CompareTag("CueBall"))
        {
            if (scoreTypes == null)
            {
                scoreTypes = new List<VisualElement>(); // Initialize the scoreTypes list
                AddScoreType("Cue Ball Rail Bounce", 100);
            }
            else
            {
                foreach (VisualElement scoreType in scoreTypes)
                {
                    if (scoreType.Q<Label>("ShotTypeHeading").text == "Cue Ball Rail Bounce")
                    {
                        scoreType.Q<Label>("ShotTypeAmount").text = (int.Parse(scoreType.Q<Label>("ShotTypeAmount").text) + 1).ToString();
                        return;
                    }
                }
            }
        }
    }

    public VisualElement AddScoreType(string header, int score, int multiplier = 0, int amount = 1)
    {
        // Ensure the template is assigned
        if (shotTypeTemplate == null)
        {
            Debug.LogError("ShotTypeTemplate is not assigned in the Inspector!");
            return null;
        }

        // Instantiate a new instance of the template
        VisualElement newShotType = shotTypeTemplate.Instantiate();

        // Populate the cloned template with data
        // Update the "header" text
        newShotType.Q<Label>("ShotTypeHeading").text = header;

        // Update the multiplier, amount, and score
        newShotType.Q<Label>("ShotTypeMult").style.display = multiplier == 0 ? DisplayStyle.None : DisplayStyle.Flex;
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

    public void UpdateScoreType(VisualElement shotType, int multiplier, int amount, int score)
    {
        if (shotTypeTemplate != null)
        {
            // Populate the cloned template with data
            shotType.Q<Label>("ShotTypeMult").text = $"+{multiplier}*";
            shotType.Q<Label>("ShotTypeAmount").text = $"{amount} x";
            shotType.Q<Label>("ShotTypeScore").text = score.ToString();

            // Add the populated template to the Score Card
            VisualElement shotScoreBackground = root.Q<VisualElement>("ShotScoreBackground");
            shotScoreBackground.Add(shotType);
        }
        else
        {
            Debug.LogError("ShotTypeTemplate is not assigned in the Inspector!");
        }
    }
}
