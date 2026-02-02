**PHASE 2 — EMOTIONAL SYSTEM UPGRADE SPECIFICATION**

*Builds fully on Phase-1 normalized-quality emotions and coin-based economy.*

**1\. Purpose of Phase 2**

Phase 1 gave you a clean, testable foundation:

* Normalized pack quality (0–1)  
* Two tracked emotions (Satisfaction & Frustration)  
* Predictable emotional deltas per pull  
* Transparent logs and analytics

Phase 2 upgrades the system **without rewriting Phase 1**.  
The goal is to add layered emotional meaning, coin-connected value perception, and pack-tier sensitivity while keeping calculations light and stable.

Phase 2 keeps Satisfaction and Frustration as the core state variables and adds *secondary signals, economic multipliers, and event-level flags* on top.

**2\. Phase-2 Architecture Overview**

Phase 2 adds three categories of upgrades:

**A. Derived Emotional Signals**  
Short-lived emotional tags that interpret S/F movement.

1. **Relief**  
2. **Disappointment**  
3. **Hope**

**B. Economic Alignment**  
Use pack cost (coins/gems) to scale emotional intensity and compute value perception.

1. **Pack Cost Multiplier**  
2. **Value Satisfaction Score**

**C. Event-Level Emotional Flags**  
One-shot markers that communicate “moments.”

1. **Lucky Moment**  
2. **Bad Beat**

These additions sit *after* Phase-1 deltas and use the same inputs:  
raw\_score, quality01, rarity values, cost\_paid.

**3\. Derived Emotional Signals**

**3.1 Relief**  
A positive temporary signal when frustration suddenly drops.  
**Trigger:** relief \= max(0, previous\_F \- current\_F)  
Clamp: 0–5  
Duration: 1–2 pulls  
**Meaning:** Player finally breaks a cold streak.

**3.2 Disappointment**  
A negative temporary signal when satisfaction falls sharply.  
**Trigger:** disappointment \= max(0, previous\_S \- current\_S)  
Clamp: 0–5  
Duration: 1–2 pulls  
**Meaning:** The player expected better from the pull.

**3.3 Hope**  
A trending emotional predictor based on recent pulls.  
Uses already logged normalized pack results.  
**Definitions:**  
hope \= average( last 5 quality01 values )  
range \= 0–1  
**Interpretation:**

* 0.7 \= optimistic  
* 0.4–0.6 \= neutral  
* \<0.3 \= cold streak mood

Hope is not stored as a new emotion value. It’s a **rolling indicator** used for UI or pacing logic.

**4\. Economic Emotional Alignment**

Phase 2 connects the emotional response to the **coin cost of packs**.  
This is critical now that Phase-2 pack types and prices diverge.

**4.1 Pack Cost Emotion Multiplier**

High-cost packs should create stronger emotional reactions.

Baseline:  
base\_cost \= Bronze\_pack\_cost

Multiplier:  
emotion\_multiplier \= 1 \+ log(cost\_paid / base\_cost)

Apply to *Phase-1 deltas*:  
S\_final \= S\_base × emotion\_multiplier  
F\_final \= F\_base × emotion\_multiplier

**Example:**  
Bronze (1000 coins): multiplier \= 1.0  
Silver (1500 coins): \~1.18  
Gold (2000 coins): \~1.30  
Premium (3000 coins): \~1.39

**Result:**  
Opening expensive packs feels more emotionally charged.

**4.2 Value Satisfaction Score**

Players naturally judge value: “Did this pack feel worth the coins?”

Phase 2 quantifies it.

Formula:  
value\_satisfaction \= (raw\_score / cost\_paid) × scaling\_factor

Recommended:  
scaling\_factor \= 1000  
Interpretation:

* High rarity pulls at low cost → High value satisfaction  
* Low rarity pulls at high cost → Low value satisfaction

This is logged per pull and can be used for tuning pack prices.

**5\. Event-Level Emotional Flags**

These flags highlight emotional moments without changing core S/F dynamics.

**5.1 Lucky Moment**

Triggered when:

* Legendary is pulled  
  **OR**  
* quality01 \> 0.90

Effect:  
S \+= lucky\_bonus  // recommended: \+2

Duration: 1 pull  
Purpose: Give the player a peak moment.

**5.2 Bad Beat**

Triggered when:

* Pack cost is high  
* AND quality01 \< 0.15  
* AND the pull contains **no Rare+** cards

Effect:  
F \+= bad\_beat\_bonus  // recommended: \+2

Duration: 1 pull

Purpose: Communicate “that felt unfair” without inflating overall frustration curves.

**6\. Integration With Phase-2 Card Types**

If Phase-2 introduces new card categories (stadiums, coaches, facilities, etc.), you **do not** change the phase-1 formula.

Instead:

**6.1 Map new card types to rarity numeric values**

Examples:

| New Type | Recommended Value |
| ----- | ----- |
| Facility | 1–3 |
| Coach | 2–4 |
| Stadium | 3–5 |

This keeps:  
raw\_score \= sum(rarity\_numeric\_values)  
quality01 \= normalize(raw\_score)  
fully intact.

**6.2 Optional: Card-Type Bonus**

For special powerful card types:  
raw\_score \+= special\_card\_bonus  // \+1 recommended  
This rewards exciting utility cards without redefining rarity.

**7\. Full Phase-2 Emotional Pipeline**

Below is the drop-in upgrade for your engine.

**Step 1\. Phase-1 raw\_score \-** Sum numeric rarity values.

**Step 2\. Phase-1 quality01 normalization \-** Same pack min/max ranges.

**Step 3\. Phase-1 base emotion deltas**  
S\_base \= quality01 × S\_max  
F\_base \= (1 \- quality01) × F\_max

**Step 4\. Apply Pack Cost Multiplier**  
S\_scaled \= S\_base × emotion\_multiplier  
F\_scaled \= F\_base × emotion\_multiplier

**Step 5\. Update Emotional State \-** Apply S\_scaled and F\_scaled to current emotion values.

**Step 6\. Compute Derived Signals**  
Relief \= change in F  
Disappointment \= change in S  
Hope \= rolling 5-pull average of quality01

**Step 7\. Event-Level Flags**  
Lucky Moment  
Bad Beat

**Step 8\. Compute Value Satisfaction \-**Stored in telemetry for balancing and pricing evaluation.

**8\. Updated Telemetry Additions (Phase 2\)**

Add the following fields to the log schema:

"phase2\_signals": {  
    "relief": float,  
    "disappointment": float,  
    "hope": float,  // rolling 5-pull average  
    "value\_satisfaction": float,  
    "emotion\_multiplier": float,  
    "lucky\_moment": boolean,  
    "bad\_beat": boolean  
}

This ensures Phase-2 integrates cleanly into your Phase-1 analytics pipeline.

**9\. Tuning Guidelines**

**Thresholds**

* Lucky threshold: quality01 ≥ 0.90  
* Bad beat threshold: quality01 ≤ 0.15  
* Hope window: 5 pulls

**Multipliers**

* Pack cost log multiplier: stable and predictable  
* Lucky bonus: \+2 satisfaction  
* Bad beat bonus: \+2 frustration

**Clamps**

* Relief/disappointment: 0–5  
* Hope: 0–1

These parameters keep the system stable and easy to tune.

**10\. Summary: What Phase-2 Achieves**

**Adds richness without complexity \-** No rewrites, just smart layers.  
**Emotion reacts to coin investment \-** Expensive packs feel more emotional.  
**Players feel both value and fairness \-** Via value satisfaction and event flags.  
**Lightweight and fully compatible with Phase-1 \-** Everything plugs into normalization and S/F.  
**Ready for Phase-3 meta-emotions \-**This sets the groundwork for deeper emotional arcs later.