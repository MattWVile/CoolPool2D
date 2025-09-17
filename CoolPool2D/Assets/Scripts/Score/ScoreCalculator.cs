using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// ScoreCalculator: tallies shot events (kiss, rails, pot) grouped by ball colour
/// and, at shot end, builds a list of multiplier entries that UI can present.
/// This version uses ONLY multiplicative factors (no additive logic).
/// Always rounds scores to whole numbers (no decimals).
/// </summary>
public class ScoreCalculator : MonoBehaviour
{
    [Serializable]
    public class MultiplierEntry
    {
        public string Label;
        public float Factor;

        public MultiplierEntry(string label, float factor)
        {
            Label = label;
            Factor = factor;
        }
    }

    [Header("Kiss settings")]
    public int kissesPerChunk = 1;
    public float kissMultiplyPerChunk = 1.3f;

    [Header("Object-ball rail settings")]
    public int objectRailsPerChunk = 3;
    public float objectRailsMultiplyPerChunk = 1.2f;

    [Header("jaw settings")]
    public int jawsPerChunk = 2;
    public float jawsMultiplyPerChunk = 1.3f;

    [Header("Pot settings")]
    public float potMultiply = 1.5f;

    [Header("Cue-ball rail settings")]
    public int cueRailsPerChunk = 5;
    public float cueRailMultiplyPerChunk = 1.2f;

    private readonly Dictionary<string, int> _kissCountsByColour = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _objRailCountsByColour = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _cueRailCountsByColour = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _jawCountsByColour = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _potCountsByColour = new(StringComparer.OrdinalIgnoreCase);

    private const string UnknownColourKey = "unknown";

    private int shotScore = 0;
    private int totalScore = 0;

    private readonly List<MultiplierEntry> currentShotMultiplierEntries = new();

    private void Start()
    {
        EventBus.Subscribe<ShotScoreTypeUpdatedEvent>(OnShotScoreTypeUpdated);
        EventBus.Subscribe<BallStoppedEvent>(OnBallsStopped);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<ShotScoreTypeUpdatedEvent>(OnShotScoreTypeUpdated);
        EventBus.Unsubscribe<BallStoppedEvent>(OnBallsStopped);
    }

    private void OnShotScoreTypeUpdated(ShotScoreTypeUpdatedEvent evt)
    {
        if (evt?.ScoreType == null || string.IsNullOrWhiteSpace(evt.ScoreType.ScoreTypeHeader))
            return;

        string header = evt.ScoreType.ScoreTypeHeader.Trim();
        string lowerHeader = header.ToLowerInvariant();
        string colourKey = ExtractColourToken(header, lowerHeader);

        if (lowerHeader.Contains("kiss"))
        {
            IncrementCount(_kissCountsByColour, colourKey);
        } 
        else if (lowerHeader.Contains("pot") || lowerHeader.Contains("pocket"))
        {
            IncrementCount(_potCountsByColour, colourKey); 
        }
        else if(lowerHeader.Contains("jaw"))
        {
            IncrementCount(_jawCountsByColour, colourKey);
        }
        else if (lowerHeader.Contains("rail"))
        {
            if (lowerHeader.Contains("white"))
                IncrementCount(_cueRailCountsByColour, colourKey);
            else
                IncrementCount(_objRailCountsByColour, colourKey);
        }
    }

    private void OnBallsStopped(BallStoppedEvent evt)
    {
        StartCoroutine(HandleScoringSequence());
    }

    private IEnumerator HandleScoringSequence()
    {
        BuildMultiplierEntries();

        if (currentShotMultiplierEntries.Count == 0)
        {
            ResetShotState();
            yield break;
        }

        CalculateShotScore();
        ScoreUIManager.Instance?.UpdateShotScore(shotScore);

        yield return StartCoroutine(ApplyMultipliersToShotScoreCoroutine());

        totalScore += shotScore;
        ScoreUIManager.Instance?.UpdateTotalScore(totalScore);
        ScoreUIManager.Instance?.ClearShotScore();
        ScoreManager.Instance.currentScoreTypes.Clear();
        ResetShotState();
        EventBus.Publish(new ScoringFinishedEvent());
    }

    private void BuildMultiplierEntries()
    {
        currentShotMultiplierEntries.Clear();

        foreach (var kv in _objRailCountsByColour)
        {
            if (objectRailsPerChunk <= 0) continue;

            int chunks = kv.Value / objectRailsPerChunk;
            for (int c = 1; c <= chunks; c++)
            {
                if (!Mathf.Approximately(objectRailsMultiplyPerChunk, 1f))
                {
                    string label = $"{ToTitleCase(kv.Key)} rail bounce chunk #{c}";
                    currentShotMultiplierEntries.Add(new MultiplierEntry(label, objectRailsMultiplyPerChunk));
                }
            }
        }

        foreach (var kv in _cueRailCountsByColour)
        {
            if (cueRailsPerChunk <= 0) continue;

            int chunks = kv.Value / cueRailsPerChunk;
            for (int c = 1; c <= chunks; c++)
            {
                if (!Mathf.Approximately(cueRailMultiplyPerChunk, 1f))
                {
                    string label = $"{ToTitleCase(kv.Key)} rail bounce chunk #{c}";
                    currentShotMultiplierEntries.Add(new MultiplierEntry(label, cueRailMultiplyPerChunk));
                }
            }
        }

        foreach (var kv in _jawCountsByColour)
        {
            if (jawsPerChunk <= 0) continue;

            int chunks = kv.Value / jawsPerChunk;
            for (int c = 1; c <= chunks; c++)
            {
                if (!Mathf.Approximately(jawsMultiplyPerChunk, 1f))
                {
                    string label = $"{ToTitleCase(kv.Key)} jaw bounce chunk #{c}";
                    currentShotMultiplierEntries.Add(new MultiplierEntry(label, jawsMultiplyPerChunk));
                }
            }
        }

        foreach (var kv in _kissCountsByColour)
        {
            if (kissesPerChunk <= 0) continue;

            int chunks = kv.Value / kissesPerChunk;
            for (int c = 1; c <= chunks; c++)
            {
                if (!Mathf.Approximately(kissMultiplyPerChunk, 1f))
                {
                    string label = $"{ToTitleCase(kv.Key)} kiss chunk #{c}";
                    currentShotMultiplierEntries.Add(new MultiplierEntry(label, kissMultiplyPerChunk));
                }
            }
        }

        foreach (var kv in _potCountsByColour)
        {
            int pots = kv.Value;
            for (int i = 1; i <= pots; i++)
            {
                if (!Mathf.Approximately(potMultiply, 1f))
                {
                    string label = $"{ToTitleCase(kv.Key)} pot #{i}";
                    currentShotMultiplierEntries.Add(new MultiplierEntry(label, potMultiply));
                }
            }
        }
    }

    private void ResetShotState()
    {
        _kissCountsByColour.Clear();
        _objRailCountsByColour.Clear();
        _cueRailCountsByColour.Clear();
        _jawCountsByColour.Clear();
        _potCountsByColour.Clear();
        currentShotMultiplierEntries.Clear();
        shotScore = 0;
    }

    private void CalculateShotScore()
    {
        shotScore = 0;

        foreach (var scoreType in ScoreManager.Instance.currentScoreTypes)
        {
            if (scoreType.IsScoreFoul)
            {
                shotScore = 0;
                return;
            }

            float raw = scoreType.NumberOfThisScoreType * scoreType.ScoreTypePoints;
            shotScore += Mathf.RoundToInt(raw);
        }
    }

    private IEnumerator ApplyMultipliersToShotScoreCoroutine()
    {
        if (currentShotMultiplierEntries.Count == 0)
            yield break;

        float uiWaitTime = Mathf.Max(0f, ScoreUIManager.Instance?.multiplierPopUpTime ?? 0.5f);
        const float smallBuffer = 0.05f;

        var entries = new List<MultiplierEntry>(currentShotMultiplierEntries);

        foreach (var mult in entries)
        {
            int amountToTrigger = GetAmountToTrigger(mult);

            ScoreUIManager.Instance?.DisplayMultiplierPopUp(amountToTrigger, mult.Label, mult.Factor);

            float raw = shotScore * mult.Factor;
            shotScore = Mathf.RoundToInt(raw);

            ScoreUIManager.Instance?.UpdateShotScore(shotScore);

            yield return new WaitForSeconds(uiWaitTime + smallBuffer);
        }
    }

    private int GetAmountToTrigger(MultiplierEntry mult)
    {
        string labelLower = (mult.Label ?? "").ToLowerInvariant();

        if (labelLower.Contains("pot")) return 1;
        if (labelLower.Contains("kiss")) return Math.Max(1, kissesPerChunk);
        if (labelLower.Contains("cue ball rail bounce")) return Math.Max(1, cueRailsPerChunk);
        if (labelLower.Contains("rail bounce")) return Math.Max(1, objectRailsPerChunk);

        return 1;
    }

    private static void IncrementCount(Dictionary<string, int> dict, string key)
    {
        if (string.IsNullOrEmpty(key)) key = UnknownColourKey;
        dict[key] = dict.TryGetValue(key, out var current) ? current + 1 : 1;
    }

    private static string ExtractColourToken(string originalHeader, string lowerHeader)
    {
        if (string.IsNullOrWhiteSpace(originalHeader)) return UnknownColourKey;

        int idx = lowerHeader.IndexOf(" ball", StringComparison.OrdinalIgnoreCase);
        string token = idx > 0
            ? originalHeader.Substring(0, idx).Trim()
            : originalHeader.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

        if (string.Equals(token, "white", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(token, "cue", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(token, "cueball", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(token, "cue-ball", StringComparison.OrdinalIgnoreCase))
        {
            return "cue ball";
        }

        return $"{token} ball";
    }

    private static string ToTitleCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
    }
}
