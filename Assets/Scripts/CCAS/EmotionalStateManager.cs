using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CCAS.Config;

/// <summary>
/// Phase 1 – Part 2 Emotional State Manager (Balanced Edition)
/// Implements: Base normalized quality → (Quality-Reset) → (Neutral Band) → (Oppositional) → (Streak) → Clamp
/// JSON tuning supported through emotion_dynamics.
/// </summary>
public class EmotionalStateManager : MonoBehaviour
{
    public static EmotionalStateManager Instance;

    [Header("Emotion Meters (0–100)")]
    [Range(0, 100)] public float frustration;
    [Range(0, 100)] public float satisfaction;

    [Header("Fallback Formula Settings (used if JSON missing)")]
    [Tooltip("Satisfaction gain at quality01 = 1.0")]
    public float S_max_Fallback = 3f;
    [Tooltip("Frustration gain at quality01 = 0.0")]
    public float F_max_Fallback = 3f;

    [Header("Debug")]
    public bool verbose = true;

    // --- Logging variables for Telemetry ---
    private float _lastFrustrationDelta;
    private float _lastSatisfactionDelta;
    private string _lastHookTriggered = "none";
    private int _lastStreakLength = 0;
    private bool _lastRareBoostApplied = false;

    // Rolling window of last N quality01 values (previous pulls only)
    private readonly Queue<float> _qualityWindow = new();

    // Quick accessors for config
    private Phase1ConfigRoot Cfg => DropConfigManager.Instance?.config;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetSession();
    }

    /// <summary>Reset both meters to 0 and clear streak window.</summary>
    public void ResetSession()
    {
        frustration = 0f;
        satisfaction = 0f;
        _lastFrustrationDelta = 0f;
        _lastSatisfactionDelta = 0f;
        _lastHookTriggered = "none";
        _lastStreakLength = 0;
        _lastRareBoostApplied = false;
        _qualityWindow.Clear();
    }

    // ------------------------------------------------------------------------
    // MAIN UPDATE
    // ------------------------------------------------------------------------
    public EmotionDeltaResult ApplyPackOutcome(string packTypeKey, List<string> rarities)
    {
        if (rarities == null) rarities = new List<string>();

        // 1) Raw score from rarities
        int rawScore = 0;
        foreach (var r in rarities)
            rawScore += GetRarityNumericValue(string.IsNullOrEmpty(r) ? "common" : r.ToLowerInvariant());

        // 2) Get pack score range
        var (minScore, maxScore) = GetPackScoreRange(packTypeKey);

        // 3) Normalize to [0,1]
        float denom = Mathf.Max(1, maxScore - minScore);
        float rawQuality = (rawScore - minScore) / denom;
        float quality01 = AdjustQualityForPack(packTypeKey, rawQuality);


        // 4) Base deltas - SIMPLIFIED: Direct quality mapping
        // This ensures satisfaction can build and frustration is proportional
        var (Smax, Fmax) = GetMaxes();
        
        // Use asymmetric curves: satisfaction builds faster, frustration builds slower
        float satisfactionCurve = Mathf.Pow(quality01, 0.7f);  // Slightly easier to gain satisfaction
        float frustrationCurve = Mathf.Pow(1f - quality01, 1.2f);  // Harder to gain frustration
        
        float dS = satisfactionCurve * Smax;
        float dF = frustrationCurve * Fmax;

        // --------------------------------------------------------------------
        // 5) Rare Card Boost - Make rare cards feel rewarding
        // --------------------------------------------------------------------
        bool hasRareOrBetter = rarities.Any(r => 
            !string.IsNullOrEmpty(r) && 
            (r.ToLowerInvariant() == "rare" || 
             r.ToLowerInvariant() == "epic" || 
             r.ToLowerInvariant() == "legendary"));
        
        if (hasRareOrBetter)
        {
            float rareBoost = 1.5f;  // 50% boost to satisfaction for rare+ cards
            dS *= rareBoost;
            _lastRareBoostApplied = true;
        }
        else
        {
            _lastRareBoostApplied = false;
        }

        // --------------------------------------------------------------------
        // 6) Quality-Driven Reduction (Fixed Logic)
        // --------------------------------------------------------------------
        var qr = Cfg?.emotion_dynamics?.quality_reset;
        if (qr != null)
        {
            // Good pulls: reduce frustration (this is the main recovery mechanism)
            if (quality01 > qr.good_threshold)
            {
                // Reduce frustration proportionally to quality
                float reduceAmount = (quality01 - qr.good_threshold) / (1f - qr.good_threshold);
                float reduce = reduceAmount * qr.R_F;
                dF = Mathf.Max(0f, dF - reduce);  // Can't go negative, just reduce frustration gain
            }
            // Bad pulls: Don't penalize satisfaction further - it's already low
            // Just reduce frustration gain slightly (less punishment for bad pulls)
            else if (quality01 < qr.bad_threshold)
            {
                // Reduce frustration gain for very bad pulls (less punishment)
                float badness = (qr.bad_threshold - quality01) / qr.bad_threshold;
                dF *= (1f - badness * 0.3f);  // Reduce frustration by up to 30% for worst pulls
            }
        }

        // --------------------------------------------------------------------
        // 7) Neutral-Band Recovery (both cool down slightly)
        // --------------------------------------------------------------------
        var nb = Cfg?.emotion_dynamics?.neutral_band;
        if (nb != null && quality01 >= nb.min && quality01 <= nb.max)
        {
            dS *= 0.85f;  // Slightly more reduction for neutral pulls
            dF *= 0.85f;
        }

        // --------------------------------------------------------------------
        // 8) Oppositional Dampening - REDUCED IMPACT
        // --------------------------------------------------------------------
        var opp = Cfg?.emotion_dynamics?.oppositional;
        float k = (opp?.k ?? 0.25f) * 0.3f;  // Reduce impact by 70% - was too strong
        float dS_opp = dS - (dF * k);
        float dF_opp = dF - (dS * k);

        // --------------------------------------------------------------------
        // 9) Streak Multiplier - BALANCED
        // --------------------------------------------------------------------
        float dS_final = dS_opp;
        float dF_final = dF_opp;
        var st = Cfg?.emotion_dynamics?.streak;
        if (st != null && st.window > 0)
        {
            float qAvg = _qualityWindow.Count > 0 ? _qualityWindow.Average() : 0.5f;
            float streak = qAvg - 0.5f; // +ve = hot, -ve = cold
            
            if (Mathf.Abs(streak) > st.threshold)
            {
                // Reduce streak multipliers - was too aggressive
                float alphaReduced = st.alpha * 0.5f;  // Half strength
                float betaReduced = st.beta * 0.5f;
                
                // Only amplify positive streaks (hot streaks), don't amplify cold streaks as much
                if (streak > 0)
                {
                    dS_final *= (1f + alphaReduced * streak);
                    dF_final *= (1f - betaReduced * streak * 0.5f);  // Less frustration reduction
                }
                else
                {
                    // Cold streaks: less punishment
                    dS_final *= (1f + alphaReduced * streak * 0.5f);  // Less satisfaction loss
                    dF_final *= (1f - betaReduced * streak * 0.7f);  // Less frustration gain
                }
            }
            _lastStreakLength = Mathf.Min(_qualityWindow.Count, st.window);
        }
        else
        {
            _lastStreakLength = _qualityWindow.Count;
        }

        // --------------------------------------------------------------------
        // 10) Apply Decay FIRST, then add deltas (allows satisfaction to build)
        // --------------------------------------------------------------------
        var caps = Cfg?.emotion_dynamics?.caps;
        float S_cap = caps?.S_cap ?? 100f;
        float F_cap = caps?.F_cap ?? 100f;

        float prevS = satisfaction;
        float prevF = frustration;

        // Apply decay BEFORE adding deltas - this allows satisfaction to accumulate
        satisfaction *= 0.98f;   // 2% decay per pull (was 0.5%, now more balanced)
        frustration  *= 0.97f;    // 3% decay per pull (was 1.5%, now more balanced)

        // Now add deltas
        satisfaction = Mathf.Clamp(satisfaction + dS_final, 0f, S_cap);
        frustration  = Mathf.Clamp(frustration  + dF_final, 0f, F_cap);

        _lastSatisfactionDelta = satisfaction - prevS;
        _lastFrustrationDelta = frustration - prevF;

        // --------------------------------------------------------------------
        // 11) Update rolling window
        // --------------------------------------------------------------------
        int targetN = st?.window ?? 5;
        EnqueueQuality(quality01, targetN);

        if (verbose)
        {
            Debug.Log($"[Emotion] pack={packTypeKey} raw={rawScore} bounds=[{minScore},{maxScore}] " +
                      $"q={quality01:F2} dS={dS_final:F2} dF={dF_final:F2} → S={satisfaction:F2} F={frustration:F2} (N={_qualityWindow.Count})");
        }

        return new EmotionDeltaResult { satisfaction = _lastSatisfactionDelta, frustration = _lastFrustrationDelta };
    }

    // ------------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------------
    private float AdjustQualityForPack(string packTypeKey, float rawQuality)
    {
        float q = Mathf.Clamp01(rawQuality);
        string k = (packTypeKey ?? "").ToLowerInvariant();

        // Bias curves by pack type
        if (k.Contains("bronze"))
            q = Mathf.Pow(q, 0.8f);   // optimistic bias (slightly inflates)
        else if (k.Contains("silver"))
            q = Mathf.Pow(q, 1.0f);   // neutral
        else if (k.Contains("gold"))
            q = Mathf.Pow(q, 1.2f);   // stricter (needs better pull to feel "good")

        return Mathf.Clamp01(q);
    }

    private (float Smax, float Fmax) GetMaxes()
    {
        var p = Cfg?.phase_1_configuration?.emotion_parameters;
        float s = p?.S_max ?? S_max_Fallback;
        float f = p?.F_max ?? F_max_Fallback;
        return (s, f);
    }

    private int GetRarityNumericValue(string rarity)
    {
        var rv = Cfg?.rarity_values;
        if (rv != null && rv.TryGetValue(rarity, out var val) && val != null)
            return Mathf.Max(1, val.numeric_value);

        return rarity switch
        {
            "uncommon"  => 2,
            "rare"      => 3,
            "epic"      => 4,
            "legendary" => 5,
            _           => 1
        };
    }

    private (int min, int max) GetPackScoreRange(string packTypeKey)
    {
        var packs = Cfg?.pack_types;
        if (!string.IsNullOrEmpty(packTypeKey) && packs != null &&
            packs.TryGetValue(packTypeKey, out var p) && p?.score_range != null)
            return (p.score_range.min_score, p.score_range.max_score);

        string k = (packTypeKey ?? string.Empty).ToLowerInvariant();
        if (k.Contains("bronze")) return (3, 7);
        if (k.Contains("silver")) return (6, 12);
        if (k.Contains("gold"))   return (9, 13);
        return (6, 12);
    }

    private void EnqueueQuality(float quality01, int maxN)
    {
        _qualityWindow.Enqueue(Mathf.Clamp01(quality01));
        while (_qualityWindow.Count > Mathf.Max(1, maxN))
            _qualityWindow.Dequeue();
    }

    // Snapshot for UI
    public (float fr, float sa) Snapshot() => (frustration, satisfaction);

    // Telemetry getters
    public float GetLastFrustrationDelta() => _lastFrustrationDelta;
    public float GetLastSatisfactionDelta() => _lastSatisfactionDelta;
    public string GetLastHookTriggered() => _lastHookTriggered;
    public int GetLastStreakLength() => _lastStreakLength;
    public bool GetLastRareBoostApplied() => _lastRareBoostApplied;
}

public struct EmotionDeltaResult
{
    public float frustration;
    public float satisfaction;
}
