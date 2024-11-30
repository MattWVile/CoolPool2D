using UnityEngine;
using UnityEngine.UIElements;

public class ScoreCardManager : MonoBehaviour
{
    public UIDocument uiDocument; // Assign this in the inspector
    private VisualElement root;
    public VisualTreeAsset shotTypeTemplate; // Assign the template .uxml here
    public VisualElement mostRecentShotAdded;

    void Start()
    {
        // Access the root element of the UI Document
        root = uiDocument.rootVisualElement;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            mostRecentShotAdded = AddScoreType("Cue Ball Wall Bounce", 2, 3, 2000);
        }        
        if (Input.GetKeyDown(KeyCode.I))
        {
            UpdateScoreType(mostRecentShotAdded, 1, 1, 1);
        }
    }

    public VisualElement AddScoreType(string header, int multiplier, int amount, int score)
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
        newShotType.Q<Label>("ShotTypeMult").text = $"+{multiplier}*";
        newShotType.Q<Label>("ShotTypeAmount").text = $"{amount} x";
        newShotType.Q<Label>("ShotTypeScore").text = score.ToString();

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
