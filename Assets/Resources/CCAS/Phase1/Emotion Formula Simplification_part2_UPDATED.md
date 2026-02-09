# Emotion Formula Simplification - Part 2 (Updated & Balanced Edition)

**Version:** 2.1 (Balanced Edition)  
**Date:** Updated after system analysis and fixes  
**Schema Version:** `ccas.p1_part2.v1`

---

## Overview

This document describes the **updated and balanced** emotion prediction system for Phase 1, Part 2. The system has been refined based on extensive testing and analysis to ensure emotions feel natural, balanced, and responsive to player actions.

### Key Improvements in This Version

1. **Fixed Base Delta Formula** - Satisfaction can now build from zero, no more dead zones
2. **Rare Card Boost** - Rare/epic/legendary cards provide meaningful satisfaction rewards
3. **Balanced Quality Reset** - Good pulls reduce frustration, bad pulls are less punishing
4. **Reduced Oppositional Impact** - Prevents feedback loops that amplified problems
5. **Balanced Streak System** - Hot streaks rewarded, cold streaks recoverable
6. **Fixed Decay Order** - Satisfaction can accumulate over multiple good pulls

---

## Calculation Pipeline

The emotion system processes each pack pull through the following steps:

```
1. Calculate Raw Quality Score
2. Normalize to [0,1] range
3. Apply Pack-Type Bias
4. Calculate Base Deltas (with asymmetric curves)
5. Apply Rare Card Boost
6. Quality-Driven Reduction (Dynamic Reset)
7. Neutral-Band Recovery
8. Oppositional Dampening (reduced impact)
9. Streak Multiplier (balanced)
10. Apply Decay (BEFORE adding deltas)
11. Add Deltas and Clamp
12. Update Rolling Window
```

---

## Step-by-Step Calculation

### Step 1: Raw Quality Score Calculation

For each pack pull, calculate the raw score by summing the numeric values of all cards:

```csharp
int rawScore = 0;
foreach (var rarity in rarities)
    rawScore += GetRarityNumericValue(rarity);
```

**Rarity Values:**
- Common: 1
- Uncommon: 2
- Rare: 3
- Epic: 4
- Legendary: 5

**Example:**
- Pull: [Common, Common, Uncommon] → rawScore = 1 + 1 + 2 = 4
- Pull: [Rare, Common, Common] → rawScore = 3 + 1 + 1 = 5

---

### Step 2: Normalize to [0,1] Range

Normalize the raw score using the pack's score range:

```csharp
float rawQuality = (rawScore - minScore) / (maxScore - minScore);
```

**Pack Score Ranges:**
- Bronze Pack: [3, 7]
- Silver Pack: [6, 12]
- Gold Pack: [9, 13]

**Example (Bronze Pack):**
- rawScore = 3 → rawQuality = (3-3)/(7-3) = 0.0
- rawScore = 5 → rawQuality = (5-3)/(7-3) = 0.5
- rawScore = 7 → rawQuality = (7-3)/(7-3) = 1.0

---

### Step 3: Apply Pack-Type Bias

Apply a power curve to adjust quality perception by pack type:

```csharp
float quality01;
if (packType == "bronze")
    quality01 = Mathf.Pow(rawQuality, 0.8f);   // Optimistic bias
else if (packType == "silver")
    quality01 = Mathf.Pow(rawQuality, 1.0f);   // Neutral
else if (packType == "gold")
    quality01 = Mathf.Pow(rawQuality, 1.2f);    // Stricter (needs better pull)
```

**Rationale:**
- Bronze packs: Players are more optimistic about lower-tier packs
- Silver packs: Neutral perception
- Gold packs: Higher expectations, need better pulls to feel "good"

---

### Step 4: Calculate Base Deltas (UPDATED - Asymmetric Curves)

**Previous Formula (Had Issues):**
```csharp
// OLD - Created dead zone, satisfaction couldn't build
dS = (quality01 - 0.5) * (2 * Smax);
dF = (0.5 - quality01) * (2 * Fmax);
```

**New Formula (Balanced):**
```csharp
// Asymmetric curves: satisfaction easier to gain, frustration harder
float satisfactionCurve = Mathf.Pow(quality01, 0.7f);
float frustrationCurve = Mathf.Pow(1f - quality01, 1.2f);

float dS = satisfactionCurve * Smax;
float dF = frustrationCurve * Fmax;
```

**Key Changes:**
- Satisfaction uses power curve with exponent 0.7 (easier to gain)
- Frustration uses power curve with exponent 1.2 (harder to gain)
- No more dead zone - satisfaction can build from 0
- Direct quality mapping instead of centered formula

**Example (Smax=3.0, Fmax=2.0):**
- quality01 = 0.0 (all commons):
  - dS = 0.0^0.7 * 3.0 = **0.0** (was -3.0)
  - dF = 1.0^1.2 * 2.0 = **2.0** (same)
  
- quality01 = 0.574 (1 rare + 2 commons):
  - dS = 0.574^0.7 * 3.0 = **2.03** (was 0.45)
  - dF = 0.426^1.2 * 2.0 = **0.72** (was -0.30)

---

### Step 5: Apply Rare Card Boost (NEW)

When rare, epic, or legendary cards appear, boost satisfaction:

```csharp
bool hasRareOrBetter = rarities.Any(r => 
    r.ToLowerInvariant() == "rare" || 
    r.ToLowerInvariant() == "epic" || 
    r.ToLowerInvariant() == "legendary");

if (hasRareOrBetter)
{
    float rareBoost = 1.5f;  // 50% boost
    dS *= rareBoost;
}
```

**Impact:**
- Makes rare cards feel immediately rewarding
- Example: dS = 2.03 → **3.05** with rare boost

---

### Step 6: Quality-Driven Reduction (FIXED Logic)

**Previous Logic (Had Issues):**
- Bad pulls (quality01 < 0.3) REDUCED satisfaction further (causing collapse)
- Good pulls reduced frustration but logic was confusing

**New Logic (Balanced):**

```csharp
var qr = config.emotion_dynamics.quality_reset;

// Good pulls: reduce frustration (main recovery mechanism)
if (quality01 > qr.good_threshold)  // default: 0.5
{
    float reduceAmount = (quality01 - qr.good_threshold) / (1f - qr.good_threshold);
    float reduce = reduceAmount * qr.R_F;  // default: 1.5
    dF = Mathf.Max(0f, dF - reduce);  // Reduce frustration gain, can't go negative
}

// Bad pulls: reduce frustration GAIN (less punishment, not more)
else if (quality01 < qr.bad_threshold)  // default: 0.3
{
    float badness = (qr.bad_threshold - quality01) / qr.bad_threshold;
    dF *= (1f - badness * 0.3f);  // Reduce frustration by up to 30% for worst pulls
}
```

**Key Changes:**
- Good pulls: Proportionally reduce frustration based on quality
- Bad pulls: Reduce frustration GAIN (less punishment), don't penalize satisfaction
- Satisfaction no longer collapses on bad pulls

**Configuration:**
```json
"quality_reset": {
  "R_S": 1.5,  // Not used in bad pull path anymore
  "R_F": 1.5,  // Frustration reduction amount for good pulls
  "good_threshold": 0.5,
  "bad_threshold": 0.3
}
```

---

### Step 7: Neutral-Band Recovery

Average pulls (quality in middle range) reduce both emotions slightly:

```csharp
var nb = config.emotion_dynamics.neutral_band;
if (quality01 >= nb.min && quality01 <= nb.max)  // default: [0.38, 0.62]
{
    dS *= 0.85f;  // Slightly more reduction
    dF *= 0.85f;
}
```

**Purpose:** Average pulls help emotions cool down naturally.

---

### Step 8: Oppositional Dampening (REDUCED Impact)

**Previous:** Full strength (k = 0.25) caused feedback loops

**New:** Reduced by 70% to prevent amplification issues

```csharp
var opp = config.emotion_dynamics.oppositional;
float k = (opp?.k ?? 0.25f) * 0.3f;  // 70% reduction

float dS_opp = dS - (dF * k);
float dF_opp = dF - (dS * k);
```

**Rationale:**
- Prevents feedback loops that amplified bad pulls
- Allows emotions to coexist more naturally
- Less artificial feeling

**Configuration:**
```json
"oppositional": {
  "k": 0.25  // Effective k = 0.075 in code (0.25 * 0.3)
}
```

---

### Step 9: Streak Multiplier (BALANCED)

**Previous:** Cold streaks amplified frustration exponentially

**New:** Balanced with asymmetric handling

```csharp
var st = config.emotion_dynamics.streak;
if (st != null && st.window > 0)
{
    float qAvg = _qualityWindow.Average();  // Average of last N pulls
    float streak = qAvg - 0.5f;  // +ve = hot, -ve = cold
    
    if (Mathf.Abs(streak) > st.threshold)  // default: 0.1
    {
        float alphaReduced = st.alpha * 0.5f;  // Half strength
        float betaReduced = st.beta * 0.5f;
        
        if (streak > 0)  // Hot streak
        {
            dS_final *= (1f + alphaReduced * streak);
            dF_final *= (1f - betaReduced * streak * 0.5f);
        }
        else  // Cold streak
        {
            // Less punishment for cold streaks
            dS_final *= (1f + alphaReduced * streak * 0.5f);
            dF_final *= (1f - betaReduced * streak * 0.7f);
        }
    }
}
```

**Key Changes:**
- Hot streaks: Full satisfaction boost, moderate frustration reduction
- Cold streaks: Reduced punishment (50% less satisfaction loss, 30% less frustration gain)
- Overall multipliers reduced by 50%

**Configuration:**
```json
"streak": {
  "window": 5,
  "alpha": 1.0,  // Effective: 0.5 in code
  "beta": 1.0,   // Effective: 0.5 in code
  "threshold": 0.1
}
```

---

### Step 10: Apply Decay (FIXED Order)

**Previous:** Decay happened AFTER adding deltas, preventing satisfaction accumulation

**New:** Decay happens BEFORE adding deltas

```csharp
// Apply decay FIRST
satisfaction *= 0.98f;   // 2% decay per pull
frustration  *= 0.97f;    // 3% decay per pull

// Then add deltas
satisfaction = Mathf.Clamp(satisfaction + dS_final, 0f, S_cap);
frustration  = Mathf.Clamp(frustration  + dF_final, 0f, F_cap);
```

**Key Changes:**
- Decay before deltas allows satisfaction to accumulate
- More balanced decay rates (2% vs 3% instead of 0.5% vs 1.5%)
- Emotions feel more natural and buildable

---

### Step 11: Update Rolling Window

Update the rolling window for streak calculations:

```csharp
int targetN = st?.window ?? 5;
_qualityWindow.Enqueue(quality01);
while (_qualityWindow.Count > targetN)
    _qualityWindow.Dequeue();
```

---

## Complete Example Calculation

**Scenario:** Bronze Pack, Pull = [Rare, Common, Common]

1. **Raw Score:** 3 + 1 + 1 = 5
2. **Normalize:** (5 - 3) / (7 - 3) = 0.5
3. **Pack Bias:** 0.5^0.8 = **0.574**
4. **Base Deltas:**
   - satisfactionCurve = 0.574^0.7 = 0.677
   - frustrationCurve = 0.426^1.2 = 0.360
   - dS = 0.677 * 3.0 = **2.03**
   - dF = 0.360 * 2.0 = **0.72**
5. **Rare Boost:** dS *= 1.5 → **3.05**
6. **Quality Reset:** quality01 (0.574) > 0.5 (good_threshold)
   - reduceAmount = (0.574 - 0.5) / (1 - 0.5) = 0.148
   - reduce = 0.148 * 1.5 = 0.222
   - dF = max(0, 0.72 - 0.222) = **0.50**
7. **Neutral Band:** 0.574 not in [0.38, 0.62] → no change
8. **Oppositional:** k = 0.075
   - dS_opp = 3.05 - (0.50 * 0.075) = **3.01**
   - dF_opp = 0.50 - (3.05 * 0.075) = **0.27**
9. **Streak:** (assuming average quality = 0.4, streak = -0.1)
   - Cold streak, reduced impact
   - dS_final = 3.01 * (1 + 0.5 * -0.1 * 0.5) = **2.94**
   - dF_final = 0.27 * (1 - 0.5 * -0.1 * 0.7) = **0.28**
10. **Decay & Apply:**
    - satisfaction *= 0.98 → then += 2.94
    - frustration *= 0.97 → then += 0.28

**Result:** Significant satisfaction gain (~2.94), minimal frustration gain (~0.28)

---

## Configuration Reference

### Emotion Parameters

```json
"emotion_parameters": {
  "S_max": 3.0,    // Max satisfaction delta per pull
  "F_max": 2.0,    // Max frustration delta per pull
  "S_cap": 100.0,  // Maximum satisfaction value
  "F_cap": 100.0   // Maximum frustration value
}
```

### Quality Reset

```json
"quality_reset": {
  "enabled": true,
  "R_S": 1.5,           // Not used in current implementation
  "R_F": 1.5,           // Frustration reduction amount for good pulls
  "good_threshold": 0.5, // Quality above this reduces frustration
  "bad_threshold": 0.3   // Quality below this gets less punishment
}
```

### Neutral Band

```json
"neutral_band": {
  "enabled": true,
  "min": 0.38,  // Lower bound of neutral quality
  "max": 0.62   // Upper bound of neutral quality
}
```

### Oppositional

```json
"oppositional": {
  "enabled": true,
  "k": 0.25  // Effective: 0.075 in code (70% reduction)
}
```

### Streak

```json
"streak": {
  "enabled": true,
  "window": 5,      // Number of previous pulls to consider
  "alpha": 1.0,     // Effective: 0.5 in code
  "beta": 1.0,      // Effective: 0.5 in code
  "threshold": 0.1  // Minimum streak magnitude to activate
}
```

---

## Comparison: Old vs New System

### Example 1: All Commons (quality01 = 0.0)

| Metric | Old System | New System |
|--------|------------|------------|
| Base dS | -3.00 | 0.00 |
| Base dF | +2.00 | +2.00 |
| Result | Satisfaction collapses | No satisfaction loss |

### Example 2: 1 Rare + 2 Commons (quality01 ≈ 0.574)

| Metric | Old System | New System |
|--------|------------|------------|
| Base dS | +0.45 | +2.03 |
| Rare Boost | N/A | +3.05 (after boost) |
| Base dF | -0.30 | +0.72 |
| Result | Tiny satisfaction gain | Significant satisfaction gain |

### Example 3: Multiple Bad Pulls

| Metric | Old System | New System |
|--------|------------|------------|
| Frustration Growth | Exponential | Linear, recoverable |
| Satisfaction | Stays at 0 | Can recover with good pulls |
| Recovery | Difficult | Natural with good pulls |

---

## Design Principles

1. **Satisfaction Should Build** - Players need to feel progress and reward
2. **Frustration Should Be Recoverable** - Bad streaks shouldn't be permanent
3. **Rare Cards Should Feel Rewarding** - Immediate satisfaction boost
4. **Balance Over Complexity** - Simpler formulas that work better
5. **Natural Feel** - Emotions should accumulate and decay naturally

---

## Testing & Tuning

### If Satisfaction Builds Too Fast:
- Reduce `S_max` in config
- Reduce satisfaction curve exponent (currently 0.7)
- Reduce rare boost (currently 1.5)

### If Frustration Still Too High:
- Reduce `F_max` in config
- Increase frustration curve exponent (currently 1.2)
- Increase decay rate (currently 0.97)

### If Rare Boost Too Strong:
- Reduce rare boost multiplier (currently 1.5 → try 1.3)

### If Streaks Too Punishing:
- Further reduce streak multipliers (currently 0.5 → try 0.3)
- Increase cold streak reduction factors

---

## Implementation Notes

- All calculations use floating-point precision
- Values are clamped to [0, cap] after each pull
- Rolling window maintains last N quality values for streak calculation
- Decay happens before delta application to allow accumulation
- Oppositional dampening is reduced by 70% to prevent feedback loops

---

## Version History

- **v2.1 (Current)**: Balanced edition with fixes
  - Fixed base delta formula
  - Added rare card boost
  - Fixed quality reset logic
  - Reduced oppositional impact
  - Balanced streak system
  - Fixed decay order

- **v2.0**: Initial Part 2 implementation
  - Quality-driven reduction
  - Neutral band recovery
  - Oppositional dampening
  - Streak multiplier

- **v1.0**: Part 1 implementation
  - Basic normalized quality scoring

---

## Conclusion

The updated emotion system provides a balanced, natural-feeling experience where:
- Satisfaction can build from zero
- Rare cards feel rewarding
- Bad pulls are less punishing
- Good pulls reduce frustration
- Emotions accumulate and decay naturally
- Streaks are balanced and recoverable

This creates a more engaging player experience where emotions respond appropriately to pack outcomes while maintaining balance and recoverability.




