using UnityEngine;
using UnityEngine.UIElements;

public class ScoreCardManager : MonoBehaviour
{
    public UIDocument uiDocument; // Assign this in the inspector
    private VisualElement root;

    void Start()
    {
        // Access the root element of the UI Document
        root = uiDocument.rootVisualElement;
    }
    public void AddScoreType(string header, int multiplier, int amount, int score)
    {
        // Find the template in the UI
        VisualElement template = root.Q<VisualElement>("ShotType");

        if (template != null)
        {
            // Clone the template
            VisualElement newShotType = template.CloneTree();

            // Populate the cloned template with data
            newShotType.Q<Label>("ShotTypeHeader").text = header;
            newShotType.Q<Label>("ShotTypeMult").text = $"x{multiplier}";
            newShotType.Q<Label>("ShotTypeAmount").text = amount.ToString();
            newShotType.Q<Label>("ShotTypeScore").text = score.ToString();

            // Add the populated template to the Score Card
            VisualElement shotScoreBackground = root.Q<VisualElement>("ShotScoreBackground");
            shotScoreBackground.Add(newShotType);
        }
        else
        {
            Debug.LogError("Template 'ShotTypeTemplate' not found!");
        }
    }
}