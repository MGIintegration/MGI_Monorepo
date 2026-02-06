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

    [Header("Phase 2 Family Levels (0–100)")]
    [Range(0, 100)] public float negative;
    [Range(0, 100)] public float positive;

    [Header("Phase 2 Positive Buckets (0–100)")]
    [Range(0, 100)] public float pos_rarity_pack;
    [Range(0, 100)] public float pos_streak;
    [Range(0, 100)] public float pos_economy;

    [Header("Phase 2 Negative Buckets (0–100)")]
    [Range(0, 100)] public float neg_rarity_pack;
    [Range(0, 100)] public float neg_streak;
    [Range(0, 100)] public float neg_economy;

    [Header("Fallback Formula Settings (used if JSON missing)")]
    [Tooltip("Positive gain at quality01 = 1.0")]
    public float P_max_Fallback = 3f;
    [Tooltip("Negative gain at quality01 = 0.0")]
    public float N_max_Fallback = 2f;

    [Header("Debug")]
    public bool verbose = true;

    // --- Logging variables for Telemetry ---
    private float _lastNegativeDelta;
    private float _lastPositiveDelta;
    private Phase2PullBreakdown _lastBreakdown;
    private int _lastStreakLength = 0;
    private bool _lastRareBoostApplied = false;

    // Rolling window of last N quality01 values (previous pulls only)
    private readonly Queue<float> _qualityWindow = new();

    // Quick accessors for config
    private CCASConfigRoot Cfg => DropConfigManager.Instance?.config;

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
        negative = 0f;
        positive = 0f;

        pos_rarity_pack = 0f;
        pos_streak = 0f;
        pos_economy = 0f;

        neg_rarity_pack = 0f;
        neg_streak = 0f;
        neg_economy = 0f;

        _lastNegativeDelta = 0f;
        _lastPositiveDelta = 0f;
        _lastBreakdown = default;
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

        // 1) Raw score from rarities (+ track max rarity numeric for pack-expectation logic)
        int rawScore = 0;
        int maxRarityNumeric = 1;
        foreach (var r in rarities)
        {
            int v = GetRarityNumericValue(string.IsNullOrEmpty(r) ? "common" : r.ToLowerInvariant());
            rawScore += v;
            if (v > maxRarityNumeric) maxRarityNumeric = v;
        }

        // 2) Get pack score range
        var (minScore, maxScore) = GetPackScoreRange(packTypeKey);

        // 3) Normalize to [0,1]
        float denom = Mathf.Max(1, maxScore - minScore);
        float rawQuality = (rawScore - minScore) / denom;
        float quality01 = AdjustQualityForPack(packTypeKey, rawQuality);

        // Phase 2: compute "general" deltas (positive/negative) first, then route into buckets.
        var (Pmax, Nmax) = GetMaxesPhase2();

        // Asymmetric curves (same shape as Phase 1, renamed)
        float positiveCurve = Mathf.Pow(quality01, 0.7f);
        float negativeCurve = Mathf.Pow(1f - quality01, 1.2f);

        float dP = positiveCurve * Pmax;
        float dN = negativeCurve * Nmax;

        // --------------------------------------------------------------------
        // Rare Card Boost (feeds routing + can slightly amplify general positive)
        // --------------------------------------------------------------------
        bool hasRareOrBetter = rarities.Any(r => 
            !string.IsNullOrEmpty(r) && 
            (r.ToLowerInvariant() == "rare" || 
             r.ToLowerInvariant() == "epic" || 
             r.ToLowerInvariant() == "legendary"));
        
        if (hasRareOrBetter)
        {
            float rareBoost = 1.15f; // smaller than Phase 1; peak is handled by routing buckets
            dP *= rareBoost;
            _lastRareBoostApplied = true;
        }
        else
        {
            _lastRareBoostApplied = false;
        }

        // Phase 2: decay bucket meters first (so they behave like “state”), then apply routed deltas.
        ApplyPhase2Decay();

        float prevPos = positive;
        float prevNeg = negative;

        // Route positive delta into positive buckets (multiple buckets can receive shares).
        var route = ComputePhase2Routing(packTypeKey, quality01, rawScore, hasRareOrBetter, maxRarityNumeric);
        float posApplied = ApplyPositiveRouting(dP, ref route);
        float negApplied = ApplyNegativeRouting(dN, ref route);

        // Recovery: very good pulls reduce negative; very bad pulls reduce positive.
        ApplyPhase2Recovery(quality01, posApplied, negApplied);

        // Recompute family levels from buckets (weights from config, default fallback).
        RecomputeFamilyLevels();

        _lastPositiveDelta = positive - prevPos;
        _lastNegativeDelta = negative - prevNeg;

        _lastBreakdown = route;
        _lastBreakdown.applied_positive_total = posApplied;
        _lastBreakdown.applied_negative_total = negApplied;
        _lastBreakdown.positive_after = positive;
        _lastBreakdown.negative_after = negative;

        // --------------------------------------------------------------------
        // Update rolling window (for streak routing)
        // --------------------------------------------------------------------
        int targetN = Cfg?.phase_2_configuration?.routing?.streak_window ?? 5;
        EnqueueQuality(quality01, targetN);

        if (verbose)
        {
            Debug.Log(
                $"[EmotionP2] pack={packTypeKey} raw={rawScore} bounds=[{minScore},{maxScore}] q={quality01:F2} " +
                $"dP={dP:F2} dN={dN:F2} → POS={positive:F2} NEG={negative:F2} | " +
                $"posBuckets[rp={pos_rarity_pack:F2},st={pos_streak:F2},eco={pos_economy:F2}] " +
                $"negBuckets[rp={neg_rarity_pack:F2},st={neg_streak:F2},eco={neg_economy:F2}]"
            );

            // Exact applied deltas (this is what should visually explain bar movement)
            Debug.Log(
                $"[EmotionP2Breakdown] POSΔ_total={posApplied:F2} (rarity_pack={route.pos_d_rarity_pack:F2}, streak={route.pos_d_streak:F2}, economy={route.pos_d_economy:F2}) emotions=[{route.pos_emotions}] | " +
                $"NEGΔ_total={negApplied:F2} (rarity_pack={route.neg_d_rarity_pack:F2}, streak={route.neg_d_streak:F2}, economy={route.neg_d_economy:F2}) emotions=[{route.neg_emotions}] | " +
                $"maxRarity={route.max_rarity_numeric} cost={route.cost_coins} valueScore={route.value_score:F2} qAvg={route.quality_avg_window:F2}"
            );
        }

        return new EmotionDeltaResult { positive = _lastPositiveDelta, negative = _lastNegativeDelta };
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

    private (float Pmax, float Nmax) GetMaxesPhase2()
    {
        var p2 = Cfg?.phase_2_configuration?.emotion_parameters;
        if (p2 != null && (p2.P_max > 0f || p2.N_max > 0f))
            return (Mathf.Max(0.1f, p2.P_max), Mathf.Max(0.1f, p2.N_max));

        // Phase 2 only: fallback to local inspector values if config missing.
        return (P_max_Fallback, N_max_Fallback);
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

    // Snapshot for UI (Phase 2: negative, positive)
    public (float neg, float pos) Snapshot() => (negative, positive);

    // Telemetry getters (Phase 2)
    public float GetLastNegativeDelta() => _lastNegativeDelta;
    public float GetLastPositiveDelta() => _lastPositiveDelta;
    public Phase2PullBreakdown GetLastBreakdown() => _lastBreakdown;
    public int GetLastStreakLength() => _lastStreakLength;
    public bool GetLastRareBoostApplied() => _lastRareBoostApplied;

    // -------------------- Phase 2 Helpers --------------------

    private void ApplyPhase2Decay()
    {
        var d = Cfg?.phase_2_configuration?.decay;
        var dp = d?.positive;
        var dn = d?.negative;

        pos_rarity_pack *= dp?.rarity_pack ?? 0.985f;
        pos_streak      *= dp?.streak      ?? 0.92f;
        pos_economy     *= dp?.economy     ?? 0.96f;

        neg_rarity_pack *= dn?.rarity_pack ?? 0.985f;
        neg_streak      *= dn?.streak      ?? 0.92f;
        neg_economy     *= dn?.economy     ?? 0.96f;

        pos_rarity_pack = Mathf.Clamp(pos_rarity_pack, 0f, 100f);
        pos_streak      = Mathf.Clamp(pos_streak, 0f, 100f);
        pos_economy     = Mathf.Clamp(pos_economy, 0f, 100f);
        neg_rarity_pack = Mathf.Clamp(neg_rarity_pack, 0f, 100f);
        neg_streak      = Mathf.Clamp(neg_streak, 0f, 100f);
        neg_economy     = Mathf.Clamp(neg_economy, 0f, 100f);
    }

    private Phase2PullBreakdown ComputePhase2Routing(string packTypeKey, float quality01, int rawScore, bool hasRareOrBetter, int maxRarityNumeric)
    {
        var r = Cfg?.phase_2_configuration?.routing;
        int cost = 0;
        if (!string.IsNullOrEmpty(packTypeKey) && Cfg?.pack_types != null && Cfg.pack_types.TryGetValue(packTypeKey, out var pack) && pack != null)
            cost = pack.cost;

        // Streak mood from rolling window
        int wN = r?.streak_window ?? 5;
        float qAvg = _qualityWindow.Count > 0 ? _qualityWindow.Average() : 0.5f;
        float coldThreshold = r?.cold_streak_threshold ?? 0.40f;
        float hotThreshold  = r?.hot_streak_threshold  ?? 0.60f;

        bool isColdMood = qAvg < coldThreshold;
        bool isHotMood  = qAvg > hotThreshold;

        // Economy value score
        float scale = r?.value_score_scale ?? 1000f;
        float valueScore = cost > 0 ? (rawScore / (float)cost) * scale : 0f;

        return new Phase2PullBreakdown
        {
            pack_type = packTypeKey ?? "",
            raw_score = rawScore,
            quality01 = quality01,
            cost_coins = cost,
            has_rare_or_better = hasRareOrBetter,
            max_rarity_numeric = Mathf.Max(1, maxRarityNumeric),
            quality_avg_window = qAvg,
            value_score = valueScore,
            cold_mood = isColdMood,
            hot_mood = isHotMood
        };
    }

    private float ApplyPositiveRouting(float dP, ref Phase2PullBreakdown b)
    {
        if (dP <= 0f) return 0f;

        var r = Cfg?.phase_2_configuration?.routing;
        float goodTh = r?.quality_good_threshold ?? 0.62f;
        float peakTh = r?.quality_peak_threshold ?? 0.85f;
        float valueGood = r?.value_good_threshold ?? 2.2f;

        // Build weights (sum to 1 when any active)
        bool rarityIsSpecialForPack = IsMaxRaritySpecialForPack(b.pack_type, b.max_rarity_numeric);
        float wRarity = (b.quality01 >= peakTh || rarityIsSpecialForPack) ? 1f : (b.quality01 >= goodTh ? 0.6f : 0f);
        float wStreak = (b.cold_mood && b.quality01 >= goodTh) ? 0.9f : 0f;
        float wEco    = (b.value_score >= valueGood) ? 0.6f : 0f;

        float sum = wRarity + wStreak + wEco;
        if (sum <= 0.0001f) return 0f;

        wRarity /= sum; wStreak /= sum; wEco /= sum;

        float dRp = dP * wRarity;
        float dSt = dP * wStreak;
        float dEc = dP * wEco;

        pos_rarity_pack = Mathf.Clamp(pos_rarity_pack + dRp, 0f, 100f);
        pos_streak      = Mathf.Clamp(pos_streak + dSt, 0f, 100f);
        pos_economy     = Mathf.Clamp(pos_economy + dEc, 0f, 100f);

        b.pos_w_rarity_pack = wRarity; b.pos_w_streak = wStreak; b.pos_w_economy = wEco;
        b.pos_d_rarity_pack = dRp;     b.pos_d_streak = dSt;     b.pos_d_economy = dEc;

        // Emotion-y labels for popups/telemetry (only include if non-zero)
        b.pos_emotions = JoinEmotions(
            dRp > 0.0001f ? "Thrill" : null,
            dSt > 0.0001f ? "Relief" : null,
            dEc > 0.0001f ? "Worth"  : null
        );
        return dRp + dSt + dEc;
    }

    private float ApplyNegativeRouting(float dN, ref Phase2PullBreakdown b)
    {
        if (dN <= 0f) return 0f;

        var r = Cfg?.phase_2_configuration?.routing;
        float badTh = r?.quality_bad_threshold ?? 0.38f;
        int highCost = r?.high_cost_threshold_coins ?? 1500;
        float valueBad = r?.value_bad_threshold ?? 1.8f;

        float wRarity = (b.quality01 <= badTh) ? 0.7f : 0f;
        float wStreak = (b.hot_mood && b.quality01 <= 0.5f) ? 0.9f : 0f;
        float wEco    = (b.cost_coins >= highCost && b.value_score > 0f && b.value_score <= valueBad) ? 1.0f : 0f;

        float sum = wRarity + wStreak + wEco;
        if (sum <= 0.0001f) return 0f;

        wRarity /= sum; wStreak /= sum; wEco /= sum;

        float dRp = dN * wRarity;
        float dSt = dN * wStreak;
        float dEc = dN * wEco;

        neg_rarity_pack = Mathf.Clamp(neg_rarity_pack + dRp, 0f, 100f);
        neg_streak      = Mathf.Clamp(neg_streak + dSt, 0f, 100f);
        neg_economy     = Mathf.Clamp(neg_economy + dEc, 0f, 100f);

        b.neg_w_rarity_pack = wRarity; b.neg_w_streak = wStreak; b.neg_w_economy = wEco;
        b.neg_d_rarity_pack = dRp;     b.neg_d_streak = dSt;     b.neg_d_economy = dEc;

        b.neg_emotions = JoinEmotions(
            dRp > 0.0001f ? "Disappointment" : null,
            dSt > 0.0001f ? "Letdown"        : null,
            dEc > 0.0001f ? "Regret"         : null
        );
        return dRp + dSt + dEc;
    }

    private static string JoinEmotions(string a, string b, string c)
    {
        // Minimal allocation join (3 max)
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b) && string.IsNullOrEmpty(c)) return "";
        if (!string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b) && string.IsNullOrEmpty(c)) return a;
        if (string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && string.IsNullOrEmpty(c)) return b;
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b) && !string.IsNullOrEmpty(c)) return c;
        var parts = new List<string>(3);
        if (!string.IsNullOrEmpty(a)) parts.Add(a);
        if (!string.IsNullOrEmpty(b)) parts.Add(b);
        if (!string.IsNullOrEmpty(c)) parts.Add(c);
        return string.Join(", ", parts);
    }

    private void ApplyPhase2Recovery(float quality01, float posApplied, float negApplied)
    {
        var rec = Cfg?.phase_2_configuration?.recovery;
        if (rec == null || !rec.enabled) return;

        // Good pull reduces negative; bad pull reduces positive.
        if (posApplied > 0f && quality01 >= (Cfg?.phase_2_configuration?.routing?.quality_good_threshold ?? 0.62f))
        {
            float reduce = posApplied * Mathf.Clamp01(rec.good_pull_reduces_negative);
            ReduceNegativeBy(reduce);
        }
        if (negApplied > 0f && quality01 <= (Cfg?.phase_2_configuration?.routing?.quality_bad_threshold ?? 0.38f))
        {
            float reduce = negApplied * Mathf.Clamp01(rec.bad_pull_reduces_positive);
            ReducePositiveBy(reduce);
        }
    }

    private void ReduceNegativeBy(float amount)
    {
        if (amount <= 0f) return;
        float total = neg_rarity_pack + neg_streak + neg_economy;
        if (total <= 0.0001f) return;

        float rp = amount * (neg_rarity_pack / total);
        float st = amount * (neg_streak / total);
        float ec = amount * (neg_economy / total);

        neg_rarity_pack = Mathf.Clamp(neg_rarity_pack - rp, 0f, 100f);
        neg_streak      = Mathf.Clamp(neg_streak - st, 0f, 100f);
        neg_economy     = Mathf.Clamp(neg_economy - ec, 0f, 100f);
    }

    private void ReducePositiveBy(float amount)
    {
        if (amount <= 0f) return;
        float total = pos_rarity_pack + pos_streak + pos_economy;
        if (total <= 0.0001f) return;

        float rp = amount * (pos_rarity_pack / total);
        float st = amount * (pos_streak / total);
        float ec = amount * (pos_economy / total);

        pos_rarity_pack = Mathf.Clamp(pos_rarity_pack - rp, 0f, 100f);
        pos_streak      = Mathf.Clamp(pos_streak - st, 0f, 100f);
        pos_economy     = Mathf.Clamp(pos_economy - ec, 0f, 100f);
    }

    private void RecomputeFamilyLevels()
    {
        var fam = Cfg?.phase_2_configuration?.families;
        float wPosRp = GetWeight(fam?.positive?.weights, "rarity_pack", 0.5f);
        float wPosSt = GetWeight(fam?.positive?.weights, "streak", 0.3f);
        float wPosEc = GetWeight(fam?.positive?.weights, "economy", 0.2f);
        float wNegRp = GetWeight(fam?.negative?.weights, "rarity_pack", 0.5f);
        float wNegSt = GetWeight(fam?.negative?.weights, "streak", 0.3f);
        float wNegEc = GetWeight(fam?.negative?.weights, "economy", 0.2f);

        float posSumW = Mathf.Max(0.0001f, wPosRp + wPosSt + wPosEc);
        float negSumW = Mathf.Max(0.0001f, wNegRp + wNegSt + wNegEc);

        positive = Mathf.Clamp((pos_rarity_pack * wPosRp + pos_streak * wPosSt + pos_economy * wPosEc) / posSumW, 0f, 100f);
        negative = Mathf.Clamp((neg_rarity_pack * wNegRp + neg_streak * wNegSt + neg_economy * wNegEc) / negSumW, 0f, 100f);
    }

    private static float GetWeight(Dictionary<string, float> weights, string key, float fallback)
    {
        if (weights != null && weights.TryGetValue(key, out var w))
            return Mathf.Max(0f, w);
        return Mathf.Max(0f, fallback);
    }

    /// <summary>
    /// Returns true when the best card in this pull is "special" for the given pack type.
    /// This prevents "Rare" from always feeling special in Silver/Gold if it's expected there.
    /// </summary>
    private static bool IsMaxRaritySpecialForPack(string packTypeKey, int maxRarityNumeric)
    {
        string k = (packTypeKey ?? "").ToLowerInvariant();

        // Expectation thresholds by pack tier:
        // - Bronze: Rare (3) is already special
        // - Silver: Epic (4) or better is special (rare is more common/expected)
        // - Gold: Legendary (5) is special (epic may be expected more often)
        if (k.Contains("bronze")) return maxRarityNumeric >= 3;
        if (k.Contains("silver")) return maxRarityNumeric >= 4;
        if (k.Contains("gold")) return maxRarityNumeric >= 5;

        // Default: treat Epic+ as special.
        return maxRarityNumeric >= 4;
    }
}

public struct EmotionDeltaResult
{
    public float negative;
    public float positive;
}

[Serializable]
public struct Phase2PullBreakdown
{
    public string pack_type;
    public int raw_score;
    public float quality01;
    public int cost_coins;
    public bool has_rare_or_better;
    public int max_rarity_numeric;

    public float quality_avg_window;
    public bool cold_mood;
    public bool hot_mood;
    public float value_score;

    // Emotion labels for UI/telemetry (human readable)
    public string pos_emotions;
    public string neg_emotions;

    // Weights and deltas applied (exact amounts)
    public float pos_w_rarity_pack;
    public float pos_w_streak;
    public float pos_w_economy;
    public float pos_d_rarity_pack;
    public float pos_d_streak;
    public float pos_d_economy;

    public float neg_w_rarity_pack;
    public float neg_w_streak;
    public float neg_w_economy;
    public float neg_d_rarity_pack;
    public float neg_d_streak;
    public float neg_d_economy;

    public float applied_positive_total;
    public float applied_negative_total;
    public float positive_after;
    public float negative_after;
}
