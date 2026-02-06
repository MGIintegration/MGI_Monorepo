using System;
using System.Collections.Generic;

namespace CCAS.Config
{
    /// <summary>
    /// Matches phase1_simplified_config.json (+ optional Phase 1 Part 2 emotion_dynamics)
    /// </summary>
    [Serializable]
    public class Phase1ConfigRoot
    {
        public string schema_version;
        public string methodology;
        public Phase1Configuration phase_1_configuration;
        public Phase2Configuration phase_2_configuration;
        public Dictionary<string, RarityValue> rarity_values;
        public Dictionary<string, PackType> pack_types;

        // NEW (optional): Phase 1 – Part 2 tuning block
        public EmotionDynamics emotion_dynamics;

        // NEW (optional): Phase 1 – Part 4 duplicate conversion tuning
        public DuplicateXP duplicate_xp;
    }

    [Serializable]
    public class Phase1Configuration
    {
        public List<string> tracked_emotions;
        public EmotionParameters emotion_parameters;
        public string session_persistence;
    }

    [Serializable]
    public class EmotionParameters
    {
        public float S_max;
        public float F_max;
    }

    // -------------------- NEW: Phase 2 – Emotion Families --------------------

    [Serializable]
    public class Phase2Configuration
    {
        public List<string> tracked_emotions;
        public Phase2EmotionParameters emotion_parameters;
        public Phase2Families families;
        public Phase2Routing routing;
        public Phase2Decay decay;
        public Phase2Recovery recovery;
    }

    [Serializable]
    public class Phase2EmotionParameters
    {
        public float P_max;
        public float N_max;
        public float P_cap = 100f;
        public float N_cap = 100f;
    }

    [Serializable]
    public class Phase2Families
    {
        public Phase2Family positive;
        public Phase2Family negative;
    }

    [Serializable]
    public class Phase2Family
    {
        public List<string> buckets;
        public Dictionary<string, float> weights;
    }

    [Serializable]
    public class Phase2Routing
    {
        public float quality_good_threshold = 0.62f;
        public float quality_peak_threshold = 0.85f;
        public float quality_bad_threshold = 0.38f;
        public int high_cost_threshold_coins = 1500;

        public int streak_window = 5;
        public float cold_streak_threshold = 0.40f;
        public float hot_streak_threshold = 0.60f;

        public float value_score_scale = 1000f;
        public float value_good_threshold = 2.20f;
        public float value_bad_threshold = 1.80f;
    }

    [Serializable]
    public class Phase2Decay
    {
        public Phase2BucketDecay positive;
        public Phase2BucketDecay negative;
    }

    [Serializable]
    public class Phase2BucketDecay
    {
        public float rarity_pack = 0.98f;
        public float streak = 0.92f;
        public float economy = 0.96f;
    }

    [Serializable]
    public class Phase2Recovery
    {
        public bool enabled = true;
        public float good_pull_reduces_negative = 0.5f;
        public float bad_pull_reduces_positive = 0.5f;
    }

    [Serializable]
    public class RarityValue
    {
        public int numeric_value;
        public string display_name;
    }

    [Serializable]
    public class PackType
    {
        public string name;
        public int cost; // coins only
        public int guaranteed_cards;
        public DropRates drop_rates;
        public ScoreRange score_range;
    }

    [Serializable]
    public class DropRates
    {
        public float common;
        public float uncommon;
        public float rare;
        public float epic;
        public float legendary;
    }

    [Serializable]
    public class ScoreRange
    {
        public int min_score;
        public int max_score;
    }

    // -------------------- NEW: Phase 1 – Part 2 --------------------

    [Serializable]
    public class EmotionDynamics
    {
        public QualityReset quality_reset;  // great/bad pulls actively cool opposite emotion
        public NeutralBand neutral_band;    // average pulls calm both emotions
        public Oppositional oppositional;   // cross-trim factor
        public Streak streak;               // rolling average multiplier (N = 5..10)
        public Caps caps;                   // clamp ranges for meters
    }

    [Serializable]
    public class QualityReset
    {
        public float good_threshold = 0.60f;
        public float bad_threshold = 0.40f;
        public float R_S = 2.0f; // satisfaction reduction constant
        public float R_F = 2.0f; // frustration reduction constant
    }

    [Serializable]
    public class NeutralBand
    {
        public float min = 0.45f;
        public float max = 0.55f;
        public float recovery = 0.8f; // reduce both S and F by this amount
    }

    [Serializable]
    public class Oppositional
    {
        public float k = 0.25f; // cross-reduction factor
    }

    [Serializable]
    public class Streak
    {
        public int window = 5;      // N previous pulls
        public float alpha = 1.0f;  // scales S delta by (1 + alpha*streak)
        public float beta = 1.0f;   // scales F delta by (1 - beta*streak)
        public float threshold = 0.10f; // optional UI label cutoff
    }

    [Serializable]
    public class Caps
    {
        public float S_cap = 100f;
        public float F_cap = 100;
    }
}

/// <summary>
/// Phase 1 – Part 4 duplicate XP tuning (XP per rarity for duplicate cards).
/// Matches the design in `Phase 1 – Part 4_ Duplicate Conversion System.pdf`.
/// </summary>
[Serializable]
public class DuplicateXP
{
    public int common_duplicate_xp = 5;
    public int uncommon_duplicate_xp = 10;
    public int rare_duplicate_xp = 25;
    public int epic_duplicate_xp = 50;
    public int legendary_duplicate_xp = 100;
}
