using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public UIDocument uiDocument; // Assign this in the inspector
    private VisualElement root;
    public VisualTreeAsset shotTypeTemplate; // Assign the template .uxml here
    public VisualTreeAsset GameOverScreen; // Assign the template .uxml here
    public VisualElement mostRecentShotAdded;

    public Button resetGameButton;
    public Button resetLastShotButton;
    public Button exitGameButton;

    public List<VisualElement> scoreTypes; // List to hold current score types

    private Coroutine multiplierPopupCoroutine;
    public float multiplierPopUpTime = 1f;

    private const string BallChildName = "RemainingShotBall";

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

    public void UpdateShotScore(float shotScoreToAdd)
    {
        string shotScoreText = root.Q<Label>("ShotScoreScore").text;
        root.Q<Label>("ShotScoreScore").text = shotScoreToAdd.ToString();
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

    public void EnableGameOverScreen(int totalScore)
    {
        VisualElement gameOverScreen = GameOverScreen.Instantiate();
        gameOverScreen.Q<Label>("TotalScoreValue").text = totalScore.ToString();
        root.Add(gameOverScreen);

        resetGameButton = gameOverScreen.Q<Button>("ResetGameButton");
        resetGameButton.RegisterCallback<ClickEvent>(ev => {
            GameManager.Instance.ResetGame();
            gameOverScreen.RemoveFromHierarchy();
        });

        resetLastShotButton = gameOverScreen.Q<Button>("RetryLastShotButton");
        resetLastShotButton.RegisterCallback<ClickEvent>(ev => {
            GameManager.Instance.RetryLastShot();
            gameOverScreen.RemoveFromHierarchy();
        });

        exitGameButton = gameOverScreen.Q<Button>("ExitGameButton");
        exitGameButton.RegisterCallback<ClickEvent>(ev => {
            GameManager.Instance.ExitGame();
            gameOverScreen.RemoveFromHierarchy();
        });

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


    public void DisplayMultiplierPopUp(int amountToTrigger, string label, float factor)
    {
        if (root == null && uiDocument != null) root = uiDocument.rootVisualElement;

        string cleanLabel = CleanMultiplierLabel(label);
        string popupText = $"{amountToTrigger}x {cleanLabel} X {factor}";

        if (multiplierPopupCoroutine != null) StopCoroutine(multiplierPopupCoroutine);
        multiplierPopupCoroutine = StartCoroutine(ShowMultiplierPopupCoroutine(popupText, multiplierPopUpTime));
    }

    private IEnumerator ShowMultiplierPopupCoroutine(string text, float duration)
    {
        if (root == null && uiDocument != null) root = uiDocument.rootVisualElement;
        if (root == null) yield break;

        var popup = root.Q<Label>("MultiplierPopUp");
        if (popup == null) yield break;

        popup.text = text;
        popup.visible = true;

        yield return new WaitForSeconds(duration);

        popup.visible = false;
        multiplierPopupCoroutine = null;
    }

    private static string CleanMultiplierLabel(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        string s = raw.Trim();

        // remove "chunk ..." suffix e.g. "Object rails chunk #1" -> "Object rails"
        int chunkIdx = s.IndexOf("chunk", StringComparison.OrdinalIgnoreCase);
        if (chunkIdx >= 0) s = s.Substring(0, chunkIdx).Trim();

        // remove trailing "#N" parts e.g. "Pot #1" -> "Pot"
        int hashIdx = s.IndexOf('#');
        if (hashIdx >= 0) s = s.Substring(0, hashIdx).Trim();

        return s;
    }

    public void UpdateRemainingShotsIcons(int remainingShots, int maxAmountOfShots)
    {
        if (root == null && uiDocument != null) root = uiDocument.rootVisualElement;
        for (int i = 1; i <= maxAmountOfShots; i++)
        {
            var shotIcon = root.Q<VisualElement>($"ShotIcon{i}");

            if (shotIcon == null)
            {
                continue;
            }

            bool shouldHaveBall = i <= remainingShots;

            var existing = shotIcon.Q<VisualElement>(BallChildName);

            if (shouldHaveBall)
            {
                if (existing == null)
                {
                    var ball = new VisualElement { name = BallChildName };
                    ball.AddToClassList("shot-ball");
                    ball.style.alignSelf = Align.Center;
                    shotIcon.Add(ball);
                }
            }
            else
            {
                if (existing != null)
                {
                    existing.RemoveFromHierarchy();
                }
            }
        }
    }
}
