# CCAS Future Plan — Ideas for the Next Person

This is less of a formal spec and more of a **conversation and idea handoff**. I’ve written it so the next person can see where things are headed, what to do next, and—just as important—**how to think** when new systems (like duplicates and emotion) get more complex. Goals first, then the details.

---

## Goals (high level)

- **Grow the economy and pack lineup:** Move from 3 pack types to **5** (Bronze, Silver, Gold, Elite, Supreme), using the structure Vrushali created. The economy and pack variety should feel clearer and more tiered.
- **Keep the system modular:** Card count per pack, pack definitions, and drop rates should stay **config‑driven** so we don’t hardcode things. When we change “how many cards per pack” or add packs, we lean on the prefabs and config we already have.
- **Leave the door open for emotion + duplicates:** Later we may want duplicate cards (and duplicate XP) to **influence positive/negative emotion** (e.g. “useful duplicate” vs “dead duplicate”). That’s a bigger design space with lots of factors—so the goal here is to **explain the idea and the mindset**, not to lock in a solution. Do the research and think through cases before diving in.

Below is the concrete “what to do next” and then the duplicate/emotion idea and how to approach it.

---

## What to do next (in order)

### 1. Add two more pack types (Elite and Supreme) so you have 5 total

**Source of truth:** Vrushali’s structure is in **`Assets/Resources/CCAS/Phase2/Phase2_CardPacksandTypes.csv`**. That file has two sections: (1) **Drop rates** — one row per pack type (Bronze, Silver, Gold, Elite, Supreme) with columns Common, Uncommon, Rare, Epic, Legendary (percentages); (2) **Best and worst outcomes** — same pack types with Worst, Best, and Total cards (3 per pack). Use this CSV when adding Elite and Supreme to `phase2_config.json`. An Excel version (**Phase2_CardPacksandTypes.xlsx**) may exist in the same folder as the source spreadsheet; the CSV is the one in the repo to reference.

From that sheet, the **5 pack types** look like this:

| Pack Type | Common | Uncommon | Rare | Epic | Legendary |
|-----------|--------|----------|------|------|-----------|
| Bronze    | 75%    | 15%      | 10%  | 0%   | 0%        |
| Silver    | 0%     | 65%      | 20%  | 10%  | 5%        |
| Gold      | 0%     | 0%       | 60%  | 30%  | 10%       |
| Elite     | 0%     | 0%       | 70%  | 15%  | 15%       |
| Supreme   | 0%     | 0%       | 0%   | 50%  | 50%       |

**Best / worst outcomes (from the sheet, still 3 cards per pack):**

| Pack Type | Worst outcome      | Best outcome              |
|-----------|--------------------|---------------------------|
| Bronze    | 3 common           | 2 uncommon, 1 rare        |
| Silver    | 3 uncommon         | 1 rare, 1 epic, 1 legendary |
| Gold      | 3 rare             | 2 epic, 1 legendary       |
| Elite     | 2 rare, 1 epic     | 1 rare, 1 epic, 1 legendary |
| Supreme   | 3 epic             | 3 legendary                |

**How to do it (modularity we already have):**

- **Config:** All pack definitions live in **`Assets/StreamingAssets/CCAS/phase2_config.json`** under `pack_types`. Right now there are `bronze_pack`, `silver_pack`, `gold_pack`. Add `elite_pack` and `supreme_pack` with the same shape:
  - `name` — display name (e.g. `"Elite Pack"`)
  - `cost` — coins (you’ll need to choose costs for Elite and Supreme to fit the economy)
  - `guaranteed_cards` — number of cards per open (see next section)
  - `drop_rates` — the percentages from the table above as decimals (e.g. Elite: `"rare": 0.7, "epic": 0.15, "legendary": 0.15`)
  - `score_range` — `min_score` and `max_score` for emotion quality (use rarity_values: e.g. 3×rare = 9, 1 rare + 1 epic + 1 legendary = 3+4+5 = 12; set min/max so quality01 makes sense for that pack)
- **UI:** **BoosterMarketAuto** builds one button per entry in `config.pack_types`. No code change needed for “more packs”—just add the two entries to JSON. It uses **BuyPackButton.prefab** and the **contentParent** you already assigned; new packs appear automatically.
- **Flow:** User taps new pack → **AcquisitionHubController.ShowPackOpening(packKey)** → **PackOpeningController.OpenPackOfType(packKey)** → **DropConfigManager.PullCards(packKey)**. So adding keys in config is enough for the flow; just keep the keys consistent (e.g. `elite_pack`, `supreme_pack`).

**Summary:** Add Elite and Supreme to `phase2_config.json` using the CSV/sheet. Choose costs and `score_range` per pack. No new prefabs or scripts required for “5 pack types”—only config and economy tuning.

---

### 2. Cards per pack (right now 3; changing when needed)

Right now every pack gives **3 cards** (`guaranteed_cards: 3` in config). The sheet also says “3 total” for all five packs. So the **immediate** next step is just adding Elite and Supreme with 3 cards each.

The system is already built so that **“cards per pack” is driven by config**, not by magic numbers in code:

- **DropConfigManager.PullCards(packKey)** uses `config.pack_types[packKey].guaranteed_cards` to decide how many cards to pull.
- **PackOpeningController** builds or reuses card slots with **BuildOrReuseCards(cards.Count)** and uses **Card.prefab** for each slot. So if you set `guaranteed_cards: 4` or `5` for a pack, the same code will instantiate more card prefabs and fill them via **CardView.Apply(card)**.

**What to use:**

- **Prefab:** **Assets/Prefabs/CCAS/Card.prefab** — one card slot (Image + TextMeshPro / CardView). The controller just instantiates as many as needed.
- **Layout:** The **cardParent** (on the pack-opening panel) holds these instances. If you increase cards per pack, you may need to adjust the layout (e.g. horizontal layout, scroll view, or different spacing) so 4–5 cards still look good. The prefab itself doesn’t change; only the parent’s layout might.
- **Config:** Change `guaranteed_cards` for the pack type in **phase2_config.json**. No changes needed in **DropConfigManager** or **PackOpeningController** logic—they already respect that value.

So: **next step** is 5 pack types with 3 cards each (per the sheet). When you later want a pack that gives 4 or 5 cards, update `guaranteed_cards` for that pack and tweak the pack-opening UI layout if needed. The modularity is already there.

---

## Duplicates and emotion (later; mindset and approach)

This part is more **idea and mindset** than a concrete “do this in week one” task.

**The idea:** Right now we have **duplicate_xp** (when you pull a card you already own, you get XP). We also have an **emotional system** (positive/negative, buckets like rarity_pack, streak, economy). A natural extension is: **should getting a duplicate push the needle toward positive or negative?**

The catch: it’s not one rule. It depends on context. For example:

- If the **player’s card is going to be upgraded** with that duplicate (e.g. we have an upgrade system that consumes duplicates), then the duplicate might be **good** — so maybe no negative emotion, or even positive.
- If the **card is not going to get upgraded** and the player “wasted” a pull on a duplicate they can’t use, that might reasonably **induce negative** emotion.

And that’s only two cases. There are many more: different upgrade systems, different game modes, whether the player was “chasing” that card, how many duplicates they already have, etc. So if I listed every factor here, it would get long and confusing—and some of it only makes sense once the **full system** (e.g. other teams, progression, upgrades) is in place.

**How I’d approach it (and how I’d want the next person to):**

1. **Don’t rush to implement.** Treat “duplicate_xp as one factor in positive/negative” as a **possible direction**, not a ticket to implement immediately. When the rest of the game (teams, progression, upgrades, etc.) is clearer, we’ll have more data points and more concrete cases.
2. **Do the research first.** Before touching the emotion formula, list the cases: “duplicate that upgrades,” “duplicate that doesn’t,” “first duplicate vs fifth duplicate,” “chasing a specific card,” etc. See what the design and other systems need. Then decide which levers (e.g. a “duplicate_value” or “upgrade_eligible” flag) the emotion system should even look at.
3. **Expect many factors.** The emotional system might eventually take in not just “was it a duplicate?” but “could it be used for upgrade?”, “how many dupes do they have?”, “pack cost vs value,” and so on. So the mindset is: **one step at a time, with the full picture in mind.** The same way we built Phase 2 (quality → buckets → recovery) with a spec and clear structure, any duplicate/emotion link should be thought through the same way—cases first, then design, then implementation.

So: consider **duplicate (and duplicate_xp)** as a **future** factor for positive/negative emotion, but treat it as something that will make more sense once the full system and other teams are in place. When you get there, do the research, map the cases, and then integrate in a modular way (e.g. extra inputs into **EmotionalStateManager** or a small sub-system that feeds into it). That’s the mindset I’d want to pass on: **understand the cases, then design; don’t code the first idea before the picture is clear.**

---

## More ideas to think about (doesn’t have to be exactly this)

Just throwing these in so you have something to riff on. None of this is a requirement—they’re small ideas you can also think about, or ignore, or change.

- **My Packs panel** — It’s there but disabled. You could use it to show “packs you own” before opening (e.g. earned from rewards or purchases), then open from there. Would need a simple model for “owned pack count” and a way to grant packs.
- **Real purchase flow** — Right now clicking a pack in the market opens it straight away. You could wire **PlayerWallet.SpendForPack(pack)** before opening (and “Not enough coins” / grey out if they can’t afford it). Cost is already in config; the flow is mostly UI + one check.
- **Telemetry as a lever** — We already log pulls and emotion to JSON/CSV. You could use that for tuning (e.g. “are players too frustrated after 5 bad pulls?”) or for A/B tests on emotion thresholds. No code change needed to *collect*; just think about how you’d analyse or feed it back into design.
- **Hooks beyond outcome_streak** — **HookOrchestrator** is set up for cooldowns and session caps. You could add more hook types (e.g. “first legendary,” “cold streak broken”) or move cooldown/cap values into config so design can tweak without code.
- **Emotion popups** — Popups (Thrill, Relief, Disappointment, etc.) could vary by duration, size, or sound per label. Small polish; **EmotionDisplayUI** already spawns from a prefab, so you’d just tune the prefab or add a tiny lookup.
- **Card art / visuals** — **Card.prefab** and **CardView** could later show card art or different frames by `uid` or rarity when you have assets. The data (uid, tier, name, team, element) is already there; it’s a matter of mapping to sprites or atlases.
- **Reset session: emotion only, not XP** — **EmotionalStateManager.ResetSession()** clears emotion state (positive/negative and buckets) but **does not** reset XP or pull history. So when you’re testing, XP and `pull_history.json` keep growing; only emotion goes back to zero if you call ResetSession. That’s worth being aware of: your XP can get pretty high after a lot of test pulls. Decide whether you want to reset emotion at all (e.g. on main menu, new day), or instead let it **decay** after a session (e.g. apply extra decay when the player leaves or after idle time), or leave it as-is. If you ever add a “reset for testing” option, you’d need to clear XP / pull history separately (e.g. **TelemetryLogger.ClearLogFile()** and whatever stores `player_xp` in PlayerPrefs).
- **Emotion paired with main gameplay (future)** — The emotion system is currently driven only by pack opens. You could later hook it into main gameplay: e.g. if the player **wins a match**, bump positive emotion; if they lose, maybe negative. That would make emotion reflect the whole session, not just pulls. Add that to the future backlog when main gameplay (matches, etc.) is in place.
- **XP beyond the number** — Hub shows total XP. You could add “XP to next level,” levels, or use XP to unlock packs/features. Duplicate XP is already awarded; it’s just a question of what the number *does* in the game.
- **Rarity-specific reveal** — **CardView** has a simple fade-in. You could do different reveal animations or sounds per rarity (e.g. legendary gets a bit more flair). Again, small polish on top of what’s there.
- **Localisation** — Pack names, emotion labels, and UI strings could come from a table or config so you can support multiple languages without touching code. Structure is already modular; this would be “where does this text come from?”

None of this needs to look exactly like what I wrote—just ideas to have in the back of your mind when you’re ready to extend things.

---

## Quick reference for this doc

| Topic | Where | What to use |
|-------|--------|-------------|
| Pack type definitions (5 packs) | **Assets/Resources/CCAS/Phase2/Phase2_CardPacksandTypes.csv** | Drop rates (Common–Legendary % per pack), best/worst outcomes, 3 cards per pack |
| Add Elite & Supreme | **StreamingAssets/CCAS/phase2_config.json** → `pack_types` | Same shape as bronze/silver/gold; add `elite_pack`, `supreme_pack` |
| Market UI (more packs) | **BoosterMarketAuto** + **BuyPackButton.prefab** | No code change; config-driven |
| Cards per pack | **phase2_config.json** → `guaranteed_cards` per pack | **PackOpeningController** + **Card.prefab** already scale with this |
| Duplicate + emotion | Future; research and cases first | **EmotionalStateManager**; design when full system (teams, upgrades) is clearer |
| **Telemetry / pull history file location** | Not in the repo; written at runtime | See **CCAS_File_Map.md** → “Where telemetry is written.” **macOS:** `~/Library/Application Support/DefaultCompany/MGI_Monorepo/Telemetry/pull_history.json`. **Windows:** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\pull_history.json`. Hard to find if you don’t know—that’s why we document it. |

If you need script or file locations, see **CCAS_File_Map.md** and **CCAS_Script_Reference_Overview.md** in this folder. For workflow, see **CCAS_Workflow_Flowchart.md**.
