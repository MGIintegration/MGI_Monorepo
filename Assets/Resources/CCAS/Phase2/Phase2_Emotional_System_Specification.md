# Phase 2 — Emotional System Specification

**Version:** 1.0  
**Schema Version:** `ccas.p2_emotion_families.v1`  
**Config File:** `StreamingAssets/CCAS/phase2_config.json`

---

## 1. Overview

Phase 2 replaces the single satisfaction/frustration pair with **emotion families**: one **positive** and one **negative** family. Each family is driven by **three basis buckets** (rarity/pack-type, streak, economy). The same “general” positive/negative deltas from a pull are **routed** into these buckets so that:

- The **display** is two bars (Positive, Negative).
- **Granularity** is in the buckets and in **emotion labels** (Thrill, Relief, Worth; Disappointment, Letdown, Regret) used for popups and telemetry.
- **No double-counting**: one positive delta and one negative delta per pull are distributed across buckets; good pulls also reduce the opposite family (recovery).

### Design principles

1. **Delta first, then organize** — Compute one positive and one negative delta per pull (from quality and pack context), then route each into the three buckets by conditions. The bar values are derived from the buckets, not from separate formulas.
2. **Pack-type expectations** — “Rare” in a Bronze pack is special; “Rare” in Silver is less so. Rarity/pack-type routing uses pack-specific thresholds so excitement/disappointment match expectations.
3. **Symmetric structure** — Positive and negative each have: rarity_pack (quality/rarity for that pack), streak (cold/hot break), economy (value for cost). Same config shape for both families.
4. **Recovery** — Very good pulls reduce the negative family; very bad pulls reduce the positive family, so the two bars move in opposite directions when pulls are clearly good or bad.

---

## 2. Architecture

### 2.1 Stored state

| Symbol | Range | Description |
|--------|--------|-------------|
| `positive` | 0–100 | Positive family level (drives green bar). |
| `negative` | 0–100 | Negative family level (drives red bar). |
| `pos_rarity_pack` | 0–100 | Positive bucket: quality/rarity for pack. |
| `pos_streak` | 0–100 | Positive bucket: cold streak broken. |
| `pos_economy` | 0–100 | Positive bucket: value-for-cost. |
| `neg_rarity_pack` | 0–100 | Negative bucket: bad-for-pack / expected better. |
| `neg_streak` | 0–100 | Negative bucket: hot streak broken. |
| `neg_economy` | 0–100 | Negative bucket: regret / wasted coins. |

### 2.2 Emotion labels (for UI and telemetry only)

| Bucket | Positive label | Negative label |
|--------|----------------|----------------|
| rarity_pack | Thrill | Disappointment |
| streak | Relief | Letdown |
| economy | Worth | Regret |

### 2.3 Config root

Runtime config is loaded from `phase2_config.json` and deserialized into `CCASConfigRoot` (see `DropConfigModels.cs`). The emotional system uses:

- `phase_2_configuration.emotion_parameters` (P_max, N_max, P_cap, N_cap)
- `phase_2_configuration.families` (weights per bucket for positive/negative)
- `phase_2_configuration.routing` (thresholds for quality, streak, economy)
- `phase_2_configuration.decay` (per-bucket decay per pull)
- `phase_2_configuration.recovery` (good pull reduces negative, bad pull reduces positive)
- `pack_types`, `rarity_values` (for score ranges, cost, rarity numeric values)

---

## 3. Calculation pipeline

Each pack open triggers `EmotionalStateManager.ApplyPackOutcome(packTypeKey, rarities)`. The pipeline:

```
1. Raw score + max rarity numeric (from rarities)
2. Normalize raw score to [0,1] (quality01) using pack score range
3. Apply pack-type bias to quality01
4. Compute general deltas dP (positive), dN (negative)
5. Rare card boost (scale dP if rare+)
6. Apply per-bucket decay (before adding deltas)
7. Compute routing context (streak mood, value score, pack cost)
8. Route dP into positive buckets (rarity_pack, streak, economy)
9. Route dN into negative buckets (rarity_pack, streak, economy)
10. Apply recovery (good pull → reduce negative; bad pull → reduce positive)
11. Recompute positive/negative from buckets (weighted combination)
12. Update rolling quality window for next pull
```

---

## 4. Formulas

### 4.1 Raw score and quality (unchanged from Phase 1)

**Rarity numeric values:** Common = 1, Uncommon = 2, Rare = 3, Epic = 4, Legendary = 5.

```csharp
rawScore = sum of rarity numeric values for all cards in the pull
maxRarityNumeric = max of those values (for pack-expectation logic)
```

**Pack score ranges (from config or fallback):**

- Bronze: [3, 7]  
- Silver: [6, 12]  
- Gold: [9, 13]

```csharp
rawQuality = (rawScore - minScore) / max(1, maxScore - minScore)
rawQuality = clamp(rawQuality, 0, 1)
```

**Pack-type bias (quality01):**

- Bronze: `quality01 = pow(rawQuality, 0.8)` (optimistic)
- Silver: `quality01 = rawQuality` (neutral)
- Gold: `quality01 = pow(rawQuality, 1.2)` (stricter)

```csharp
quality01 = clamp(quality01, 0, 1)
```

### 4.2 General deltas (positive and negative)

Parameters from config: `P_max`, `N_max` (defaults 3 and 2 if missing).

```csharp
positiveCurve = pow(quality01, 0.7f)
negativeCurve = pow(1f - quality01, 1.2f)
dP = positiveCurve * P_max
dN = negativeCurve * N_max
```

If the pull contains any Rare, Epic, or Legendary:

```csharp
dP *= 1.15f   // rare boost (config could expose this later)
```

### 4.3 Decay (before adding deltas)

Applied to all six bucket meters each pull. Defaults from config (e.g. 0.985, 0.92, 0.96 per bucket).

```csharp
pos_rarity_pack *= decay_positive.rarity_pack
pos_streak      *= decay_positive.streak
pos_economy     *= decay_positive.economy
(analogous for negative)
// then clamp each to [0, 100]
```

### 4.4 Routing context (per pull)

- **Cost:** from `pack_types[packTypeKey].cost`.
- **Value score:** `valueScore = (rawScore / cost) * value_score_scale` (e.g. scale 1000).
- **Rolling average quality:** `qAvg = average(last N quality01 values)` (N = streak_window, e.g. 5).
- **Cold mood:** `qAvg < cold_streak_threshold` (e.g. 0.4).
- **Hot mood:** `qAvg > hot_streak_threshold` (e.g. 0.6).
- **Rarity special for pack:** see §4.5.

### 4.5 Pack-type expectation (rarity “special” for pack)

So that “Rare” in Bronze feels special but “Rare” in Silver does not automatically:

```csharp
IsMaxRaritySpecialForPack(packTypeKey, maxRarityNumeric):
  if bronze → maxRarityNumeric >= 3
  if silver → maxRarityNumeric >= 4
  if gold   → maxRarityNumeric >= 5
  default   → maxRarityNumeric >= 4
```

Used only when deciding how much of the positive delta goes to the **rarity_pack** bucket (and for “Thrill” vs generic good pull).

### 4.6 Positive routing (distribute dP into three buckets)

Thresholds (config): `quality_good_threshold` (e.g. 0.62), `quality_peak_threshold` (e.g. 0.85), `value_good_threshold` (e.g. 2.2).

- **Rarity weight (raw):**  
  - 1.0 if `quality01 >= peakTh` OR `IsMaxRaritySpecialForPack(pack, maxRarityNumeric)`  
  - 0.6 if `quality01 >= goodTh` (else 0).
- **Streak weight (raw):** 0.9 if cold_mood AND `quality01 >= goodTh`, else 0.
- **Economy weight (raw):** 0.6 if `value_score >= value_good_threshold`, else 0.

Weights are normalized to sum to 1 (if sum &gt; 0). Then:

```csharp
dRp = dP * wRarity,  dSt = dP * wStreak,  dEc = dP * wEco
pos_rarity_pack += dRp,  pos_streak += dSt,  pos_economy += dEc
(clamp each to [0, 100])
```

If all raw weights are 0, no positive delta is applied (sum &lt;= epsilon).

### 4.7 Negative routing (distribute dN into three buckets)

Thresholds: `quality_bad_threshold` (e.g. 0.38), `high_cost_threshold_coins` (e.g. 1500), `value_bad_threshold` (e.g. 1.8).

- **Rarity weight (raw):** 0.7 if `quality01 <= badTh`, else 0.
- **Streak weight (raw):** 0.9 if hot_mood AND `quality01 <= 0.5`, else 0.
- **Economy weight (raw):** 1.0 if `cost >= high_cost` AND `value_score > 0` AND `value_score <= value_bad`, else 0.

Normalize to sum 1; then:

```csharp
neg_rarity_pack += dRp,  neg_streak += dSt,  neg_economy += dEc
(clamp to [0, 100])
```

### 4.8 Recovery

If `recovery.enabled`:

- When **positive delta was applied** and `quality01 >= quality_good_threshold`:  
  `reduce = posApplied * good_pull_reduces_negative` (e.g. 0.5).  
  Subtract `reduce` from the negative family in proportion to current bucket values (so negative bar goes down).
- When **negative delta was applied** and `quality01 <= quality_bad_threshold`:  
  `reduce = negApplied * bad_pull_reduces_positive`.  
  Subtract from positive family in proportion to bucket values.

Reduction is split across the three buckets of that family so all decrease proportionally.

### 4.9 Family level from buckets

Config gives weights per bucket (e.g. rarity_pack 0.5, streak 0.3, economy 0.2). Defaults if missing: 0.5, 0.3, 0.2.

```csharp
positive = clamp(
  (pos_rarity_pack * wRp + pos_streak * wSt + pos_economy * wEc) / (wRp + wSt + wEc),
  0, 100
)
negative = clamp(
  (neg_rarity_pack * wRp + neg_streak * wSt + neg_economy * wEc) / (wRp + wSt + wEc),
  0, 100
)
```

These are the two values shown on the bars and logged in telemetry.

---

## 5. Edge cases and considerations

### 5.1 No positive or no negative weight active

- If all positive raw weights are 0, `posApplied = 0`; no positive buckets increase. The positive bar can still decrease due to recovery if the pull was bad.
- If all negative raw weights are 0, `negApplied = 0`; no negative buckets increase. The negative bar can still decrease due to recovery if the pull was good.

### 5.2 Multiple buckets active

- Several buckets can have non-zero weight in one pull (e.g. Thrill + Relief when a rare breaks a cold streak). The **same** dP (or dN) is split among them; total applied to the family equals dP (or dN), so there is no double-counting.

### 5.3 Empty or invalid pull

- If `rarities` is null or empty, it is treated as an empty list; rawScore and maxRarityNumeric still computed (e.g. 0 and 1). Quality can be 0 or low, so negative delta may apply and positive may not.

### 5.4 Missing or partial config

- If `phase_2_configuration` or nested blocks are missing, code uses hardcoded fallbacks (e.g. P_max 3, N_max 2, thresholds as in §4.6–4.7, decay 0.985/0.92/0.96). Family weights default to 0.5 / 0.3 / 0.2.
- Pack score range and cost come from `pack_types`; if pack key is unknown, fallback ranges and cost 0 are used (valueScore then 0).

### 5.5 Pack-type and unknown pack keys

- Pack type is matched case-insensitively and by substring (e.g. "bronze", "silver", "gold"). Unknown pack types use Silver-like fallbacks and “special” rarity default (Epic+).

### 5.6 Rolling window

- The quality window is updated **after** applying deltas and recovery. So the “streak mood” (cold/hot) for the **current** pull uses only **previous** pulls. Window size is `routing.streak_window` (e.g. 5).

### 5.7 Session reset

- `ResetSession()` sets all six buckets and positive/negative to 0 and clears the quality window. Typically called at session start so each session starts from a neutral state.

---

## 6. Telemetry and logging

### 6.1 Per-pull breakdown (`Phase2PullBreakdown`)

After each pull, the manager stores a breakdown used for telemetry and popups:

- **Context:** pack_type, raw_score, quality01, cost_coins, has_rare_or_better, max_rarity_numeric, quality_avg_window, cold_mood, hot_mood, value_score.
- **Emotion labels:** pos_emotions, neg_emotions (e.g. "Thrill, Relief" or "Disappointment").
- **Applied deltas:** pos_d_rarity_pack, pos_d_streak, pos_d_economy, neg_d_rarity_pack, neg_d_streak, neg_d_economy.
- **Weights:** pos_w_*, neg_w_* (for debugging).
- **Totals:** applied_positive_total, applied_negative_total, positive_after, negative_after.

### 6.2 Console (when `verbose` is true)

- One line: pack, rawScore, bounds, quality01, dP, dN, final positive/negative, and all six bucket values.
- Second line: POSΔ_total and NEGΔ_total with per-bucket deltas, emotion labels, maxRarity, cost, valueScore, qAvg.

### 6.3 CSV export

File: `PHASE_2_EMOTIONAL_STATE_LOG.csv` (under application telemetry folder).

Columns include: log_id, timestamp, session_id, player_id, event_type, negative_after, positive_after, negative_delta, positive_delta, pos_d_rarity_pack, pos_d_streak, pos_d_economy, neg_d_rarity_pack, neg_d_streak, neg_d_economy.

The same numbers that move the bars are what get logged (and drive popups).

### 6.4 JSON pull log

Each pack pull log entry includes `positive_after`, `negative_after`, and `phase2_breakdown` (the full breakdown struct) so analysts can see both family levels and which emotions (and bucket deltas) fired.

---

## 7. UI behaviour

### 7.1 Bars

- **EmotionDisplayUI** reads `EmotionalStateManager.Snapshot()` → (negative, positive).
- Two bars (e.g. red for negative, green for positive) are driven by these two values (0–100), typically with smooth lerp.
- No satisfaction/frustration variables; only positive and negative.

### 7.2 Popups

- When a pull has non-zero bucket deltas, **EmotionDisplayUI** spawns short-lived text under the corresponding bar:
  - Positive: Thrill, Relief, Worth (for rarity_pack, streak, economy).
  - Negative: Disappointment, Letdown, Regret.
- Popups use the **exact** breakdown deltas (e.g. show only if pos_d_rarity_pack &gt; epsilon). Multiple emotions in one family are stacked vertically (e.g. `popupStackSpacing`) so they don’t overlap.
- Popups fade and rise over a fixed lifetime (e.g. 1.5 s), then are destroyed. When the panel is disabled (e.g. navigating away), any active popup clones under the anchors are cleared so they don’t persist.

### 7.3 Drop History

- Displays latest pull’s `positive_after` and `negative_after` as “Positive: X.XX” and “Negative: X.XX”.
- To avoid “one letter per row” when the label RectTransform is narrow, the controller sets TextMeshPro overflow to Overflow and enforces a minimum width when updating these labels.

---

## 8. Config reference (phase_2_configuration)

| Block | Purpose |
|--------|--------|
| `emotion_parameters` | P_max, N_max, P_cap, N_cap (max deltas and caps; caps currently 100). |
| `families.positive/negative` | buckets list and weights (rarity_pack, streak, economy) for combining buckets into family level. |
| `routing` | quality_good_threshold, quality_peak_threshold, quality_bad_threshold, high_cost_threshold_coins, streak_window, cold_streak_threshold, hot_streak_threshold, value_score_scale, value_good_threshold, value_bad_threshold. |
| `decay.positive/negative` | rarity_pack, streak, economy multipliers per pull (e.g. 0.985, 0.92, 0.96). |
| `recovery` | enabled, good_pull_reduces_negative, bad_pull_reduces_positive (e.g. 0.5, 0.5). |

Shared data (pack_types, rarity_values, duplicate_xp) lives at the root of the same config file.

---

## 9. Summary

- **One general positive and one general negative delta** per pull, from quality and rare boost.
- **Decay** is applied to all buckets first; then deltas are **routed** into three positive and three negative buckets by quality, streak mood, and economy.
- **Recovery** reduces the opposite family when pulls are clearly good or bad.
- **Family levels** are a weighted combination of the three buckets and drive the two bars.
- **Emotion labels** (Thrill, Relief, Worth; Disappointment, Letdown, Regret) are derived from which buckets received deltas and are used for **popups and telemetry**; the **display bars** use only the positive/negative values.
- **Pack-type expectations** ensure “special” rarity depends on pack (e.g. Rare in Bronze vs Silver).
- **Edge cases** (no active weights, missing config, unknown pack, session reset, rolling window) are handled with fallbacks and clear ordering so behaviour stays consistent and auditable.

This document and the code in `EmotionalStateManager.cs`, `EmotionDisplayUI.cs`, `DropHistoryController.cs`, and `TelemetryLogger.cs` are the single reference for how the Phase 2 emotional system is built and how to interpret formulas, workflow, and edge cases.
