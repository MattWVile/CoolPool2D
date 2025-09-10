using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MultiplierCalculator: tallies shot events (kiss, rails, pot) and, at shot end,
/// builds a list of multiplier entries (only entries that change the multiplier)
/// that UI can present.
/// </summary>
public class MultiplierCalculator : MonoBehaviour
{
    [Serializable]
    public class MultiplierEntry
    {
        public string Label;
        public float Factor; // subtotal multiplicative factor (1f = no effect)

        public MultiplierEntry(string label, float factor)
        {
            Label = label;
            Factor = factor;
        }
    }

    [Header("Kiss settings")]
    public int kissesPerChunk = 1;            // every N kisses = 1 chunk
    public float kissAdditionPerChunk = 0f;   // additive subtotal applied when a chunk completes (1 + addition)
    public float kissMultiplyPerChunk = 2f;   // multiplicative factor applied when a chunk completes

    [Header("Object-ball rail settings")]
    public int railsPerChunk = 3;             // every N rails = 1 chunk
    public float railsAdditionPerChunk = 1f;  // additive subtotal for each chunk (1 + addition)
    public float railsMultiplyPerChunk = 2f;  // multiplicative factor for each chunk

    [Header("Pot settings")]
    public float potAddition = 4f;            // additive subtotal per pot (1 + potAddition)
    public float potMultiply = 2f;            // multiplicative factor per pot (1 = none)

    [Header("Cue-ball rail settings")]
    public int cueRailsPerChunk = 5;
    public float cueRailAdditionPerChunk = 0f;
    public float cueRailMultiplyPerChunk = 2f;

    // runtime counters for the current shot
    private int _kissCount = 0;
    private int _objRailCount = 0;
    private int _cueRailCount = 0;
    private int _potsCount = 0;

    // entries to present to UI (only entries where Factor != 1)
    private readonly List<MultiplierEntry> _multipliersToPresent = new List<MultiplierEntry>();

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

    private void ResetShotState()
    {
        _kissCount = 0;
        _objRailCount = 0;
        _cueRailCount = 0;
        _potsCount = 0;
        _multipliersToPresent.Clear();
    }

    // Called whenever ScoreManager publishes a shot score type update
    private void OnShotScoreTypeUpdated(ShotScoreTypeUpdatedEvent evt)
    {
        if (evt?.ScoreType == null) return;

        string header = (evt.ScoreType.ScoreTypeHeader ?? "").Trim();
        if (string.IsNullOrEmpty(header)) return;

        string lower = header.ToLowerInvariant();

        // update counters based on header keywords (case-insensitive)
        if (lower.Contains("kiss"))
        {
            _kissCount++;
        }

        if (lower.Contains("pot") || lower.Contains("pocket"))
        {
            _potsCount++;
        }

        if (lower.Contains("rail"))
        {
            // heuristic: if header contains 'cue', treat as cue-ball rail
            if (lower.Contains("cue"))
                _cueRailCount++;
            else
                _objRailCount++;
        }
    }

    // Called when shot ends — compute multiplier breakdown and publish for UI
    private void OnBallsStopped(BallStoppedEvent evt)
    {
        _multipliersToPresent.Clear();

        // OBJECT-BALL RAILS: one entry per chunk only (no-op rails omitted)
        if (_objRailCount > 0 && railsPerChunk > 0)
        {
            int railChunks = _objRailCount / railsPerChunk;
            for (int c = 1; c <= railChunks; c++)
            {
                float subtotal = 1f;
                if (railsAdditionPerChunk != 0f) subtotal *= (1f + railsAdditionPerChunk);
                if (railsMultiplyPerChunk != 1f) subtotal *= railsMultiplyPerChunk;

                if (!Mathf.Approximately(subtotal, 1f))
                    _multipliersToPresent.Add(new MultiplierEntry($"Object rails chunk #{c}", subtotal));
            }
        }

        // CUE-BALL RAILS: one entry per chunk only
        if (_cueRailCount > 0 && cueRailsPerChunk > 0)
        {
            int cueChunks = _cueRailCount / cueRailsPerChunk;
            for (int c = 1; c <= cueChunks; c++)
            {
                float subtotal = 1f;
                if (cueRailAdditionPerChunk != 0f) subtotal *= (1f + cueRailAdditionPerChunk);
                if (cueRailMultiplyPerChunk != 1f) subtotal *= cueRailMultiplyPerChunk;

                if (!Mathf.Approximately(subtotal, 1f))
                    _multipliersToPresent.Add(new MultiplierEntry($"Cue rails chunk #{c}", subtotal));
            }
        }

        // KISSES: chunk-based entries only
        if (_kissCount > 0 && kissesPerChunk > 0)
        {
            int kissChunks = _kissCount / kissesPerChunk;
            for (int c = 1; c <= kissChunks; c++)
            {
                float subtotal = 1f;
                if (kissAdditionPerChunk != 0f) subtotal *= (1f + kissAdditionPerChunk);
                if (kissMultiplyPerChunk != 1f) subtotal *= kissMultiplyPerChunk;

                if (!Mathf.Approximately(subtotal, 1f))
                    _multipliersToPresent.Add(new MultiplierEntry($"Kiss chunk #{c}", subtotal));
            }
        }

        // POTS: one entry per pot only if it changes multiplier
        if (_potsCount > 0)
        {
            for (int i = 1; i <= _potsCount; i++)
            {
                float subtotal = 1f;
                if (potAddition != 0f) subtotal *= (1f + potAddition);
                if (potMultiply != 1f) subtotal *= potMultiply;

                if (!Mathf.Approximately(subtotal, 1f))
                    _multipliersToPresent.Add(new MultiplierEntry($"Pot #{i}", subtotal));
            }
        }

        // If no multiplier-contributing entries, clear and exit
        if (_multipliersToPresent.Count == 0)
        {
            ResetShotState();
            return;
        }

        // Publish result for UI (UI can animate these entries)
        var evtOut = new ShotMultipliersCalculatedEvent
        {
            Sender = this,
            Multipliers = new List<MultiplierEntry>(_multipliersToPresent)
        };
        EventBus.Publish(evtOut);

        ResetShotState();
    }
}
