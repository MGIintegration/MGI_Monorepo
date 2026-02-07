# CCAS Scripts — Detailed Reference

This document provides detailed information for each script in **Assets/Scripts/CCAS**, to support onboarding and ongoing development. Use **CCAS_Script_Reference_Overview.md** for a quick script lookup table.

**For every CCAS file and folder (StreamingAssets, Resources, Prefabs, Scenes, and Scripts) and what each is used for, see CCAS_File_Map.md** in this folder.

---

## 1. AcquisitionHubController

**File:** `Assets/Scripts/CCAS/AcquisitionHubController.cs`

### What it does

Central controller for the acquisition flow. Ensures only one main panel is visible at a time: Hub, Market, Pack Opening, Drop History, or My Packs. Handles navigation buttons and updates the hub’s coins/XP display.

### Key members

- **UI:** `coinsText`, `xpText` (TMP_Text)
- **Buttons:** `goToMarketButton`, `myPacksButton`
- **Panels:** `hubPanel`, `marketPanel`, `packPanel`, `dropHistoryPanel`, `myPacksPanel`

### Flow

- `Start`: Wires button listeners, sets initial coins text, calls `UpdateXPDisplay()`, then `ShowHub()`.
- **Navigation:** `ShowHub()`, `ShowMarket()`, `ShowMyPacks()`, `ShowPackOpening(packKey)`, `ShowHistory()`.
- `SetActivePanel(active)` turns off all panels except the one passed in.
- XP comes from `PlayerPrefs.GetInt("player_xp", 0)`.

### Notes

- My Packs is currently disabled (`myPacksButton.interactable = false`).
- `ShowPackOpening(packKey)` finds `PackOpeningController` on `packPanel` and calls `OpenPackOfType(packKey)`.

---

## 2. BoosterMarketAuto

**File:** `Assets/Scripts/CCAS/BoosterMarketAuto.cs`

### What it does

Builds the Booster Market UI from the drop config: one button per pack type. Clicking a pack calls the hub to open that pack (no purchase flow yet; opens immediately).

### Key members

- `packButtonPrefab` – prefab instantiated for each pack
- `contentParent` – parent Transform for the buttons (e.g. scroll content)

### Flow

- `Start` → `GeneratePackButtons()`.
- Reads `DropConfigManager.Instance.config.pack_types`, clears `contentParent` children, then for each pack: instantiate prefab, set label to `"{pack.name} ({pack.cost} coins)"`, set button click to `TryOpenPack(packKey)`.
- `TryOpenPack(packKey)` finds `AcquisitionHubController` and calls `ShowPackOpening(packKey)`.

### Dependencies

- `DropConfigManager.Instance` and its `config.pack_types` (from `phase2_config.json`).

---

## 3. Card and CardsCatalog

**File:** `Assets/Scripts/CCAS/Card.cs`

### What they do

- **Card:** Data model for one card: `uid`, `cardTier` (1–5), `name`, `team`, `element`, `position5`. Helpers: `GetRarityString()` (tier → "common" … "legendary"), `Card.RarityStringToTier(rarity)` (string → 1–5).
- **CardsCatalog:** Root for catalog JSON: `public Card[] cards;`.

### Usage

Used by `CardCatalogLoader`, `PackOpeningController`, `CardView`, `TelemetryLogger`, and anywhere card data is passed (e.g. `List<Card>`).

---

## 4. CardCatalogLoader

**File:** `Assets/Scripts/CCAS/CardCatalogLoader.cs`

### What it does

Singleton that loads the cards catalog from `StreamingAssets/CCAS/cards_catalog.json`, builds a tier index, and provides random card lookups by tier or rarity.

### Key members

- `Instance` – singleton
- `catalog` – loaded `CardsCatalog` (read-only at runtime)
- Internal: `_cardsByTier` (Dictionary<int, List<Card>>)

### Main API

- `GetRandomCardByTier(int tier)` – random card of that tier; falls back to tier 1 if tier missing.
- `GetRandomCardByRarity(string rarity)` – uses `Card.RarityStringToTier` then `GetRandomCardByTier`.
- `GetCardsByTier(int tier)` – returns a copy of the list of cards for that tier.

### Lifecycle

- `Awake`: singleton setup, `DontDestroyOnLoad`, `LoadCatalog()` → `BuildTierIndex()`.

### Dependencies

- JSON at `Application.streamingAssetsPath/CCAS/cards_catalog.json`; Newtonsoft.Json.

---

## 5. CardView

**File:** `Assets/Scripts/CCAS/CardView.cs`

### What it does

Handles the visual representation of a single card slot on the Pack Opening screen: rarity color, text (name and optionally team/element/position), and optional fade-in.

### Key members

- **References:** `frame` (Image), `label` (TMP, primary), `teamLabel`, `elementLabel`, `positionLabel`, `cg` (CanvasGroup for fade).
- **Animation:** `revealAlpha`, `revealSpeed`.

### API

- `Apply(string rarityLower)` – sets frame color and label to rarity (e.g. "RARE"); starts fade if `cg` set.
- `Apply(Card card)` – sets frame color from card rarity; sets primary label to card name; if separate labels exist, uses them for team/element/position; otherwise formats one line as "Name\nTeam | Element | Position". Then fade if `cg` set.

### Notes

- `Awake` auto-finds `frame`, `label`, `cg` if null.
- Colors via `RarityColorUtility.GetColorForRarity(rarity)`.

---

## 6. CCASConfigLoader

**File:** `Assets/Scripts/CCAS/config/CCASConfigLoader.cs`

### What it does

Static utility to load a JSON config file from `StreamingAssets/CCAS/` and deserialize it to a given type `T`.

### API

- `Load<T>(string fileName)` – path = `StreamingAssets/CCAS/{fileName}`; reads file, `JsonConvert.DeserializeObject<T>(json)`, returns default if file missing.

### Usage

Generic; used wherever a CCAS config JSON needs to be loaded by name (e.g. optional or alternate configs). Main drop/pack config is loaded by `DropConfigManager` directly from `phase2_config.json`.

---

## 7. DropConfigManager

**File:** `Assets/Scripts/CCAS/DropConfigManager.cs`

### What it does

Singleton that loads `phase2_config.json` and performs weighted card pulls: by rarity only (`PullCardRarities`) or full card objects (`PullCards` via `CardCatalogLoader`).

### Key members

- `Instance`, `config` (CCASConfigRoot – pack_types, rarity_values, etc.)

### Main API

- `PullCardRarities(string packKey)` – returns `List<string>` of rarity strings for that pack’s `guaranteed_cards` and `drop_rates`.
- `PullCards(string packKey)` – same roll logic but uses `CardCatalogLoader.GetRandomCardByRarity` for each roll and returns `List<Card>`.

### Internals

- `WeightedRoll(DropRates)` – random roll over common/uncommon/rare/epic/legendary weights.

### Lifecycle

- `Awake`: singleton, `DontDestroyOnLoad`, `LoadConfig()` from `StreamingAssets/CCAS/phase2_config.json`.

---

## 8. DropConfigModels

**File:** `Assets/Scripts/CCAS/DropConfigModels.cs`

### What it does

Defines all data classes used to deserialize `phase2_config.json` and related config.

### Key types (namespace CCAS.Config)

- **CCASConfigRoot:** `schema_version`, `methodology`, `phase_2_configuration`, `rarity_values`, `pack_types`, `duplicate_xp`.
- **Phase2Configuration:** `tracked_emotions`, `emotion_parameters`, `families`, `routing`, `decay`, `recovery`.
- **Phase2EmotionParameters:** P_max, N_max, P_cap, N_cap.
- **Phase2Families / Phase2Family:** positive/negative buckets and weights.
- **Phase2Routing:** quality thresholds, streak window and cold/hot thresholds, high_cost_threshold_coins, value_score_scale and value good/bad thresholds.
- **Phase2Decay / Phase2BucketDecay:** per-bucket decay (e.g. rarity_pack, streak, economy).
- **Phase2Recovery:** enabled, good_pull_reduces_negative, bad_pull_reduces_positive.
- **RarityValue:** numeric_value, display_name.
- **PackType:** name, cost, guaranteed_cards, drop_rates, score_range.
- **DropRates:** common, uncommon, rare, epic, legendary (floats).
- **ScoreRange:** min_score, max_score.

### Global (no namespace)

- **DuplicateXP:** common_duplicate_xp through legendary_duplicate_xp (Phase 1 Part 4 duplicate conversion).

Used by `DropConfigManager`, `EmotionalStateManager`, and `TelemetryLogger` (duplicate XP).

---

## 9. DropHistoryController

**File:** `Assets/Scripts/CCAS/DropHistoryController.cs`

### What it does

Powers the Drop History panel: lists recent pack pulls (card names, rarity colors, duplicate + XP), and shows the latest positive/negative emotional values (Phase 2) in fixed labels. Refreshes when the panel is shown and when a new pull is logged.

### Key members

- **Navigation:** `hubPanel`, `dropHistoryPanel`, `backToHubButton` (back → hub).
- **Content:** `contentParent`, `resultTemplate`, `scrollRect`.
- **Fixed labels:** `posAfterLabel`, `negAfterLabel` (Positive_after / Negative_after text, updated in place).
- **Display:** `recentPullsToShow` (default 3).

### Flow

- Subscribes to `TelemetryLogger.Instance.OnPullLogged` to call `RefreshDropHistory()`.
- `RefreshDropHistory()` → `PopulateAndScroll()` coroutine: clear content (except template), get `TelemetryLogger.GetRecent(recentPullsToShow)`, for each log instantiate entries from card names (or rarity fallback), color by rarity, show duplicate + XP when present. After the loop, set `posAfterLabel` / `negAfterLabel` to latest `positive_after` / `negative_after`, force layout rebuild, scroll to top.

### Dependencies

- `TelemetryLogger`, `RarityColorUtility`, `AcquisitionHubController`.

---

## 10. EmotionalStateManager

**File:** `Assets/Scripts/CCAS/EmotionalStateManager.cs`

### What it does

Phase 2 emotional state engine. For each pack open it: computes a single positive and negative delta from pull quality, routes them into three buckets per family (rarity_pack, streak, economy), applies decay and recovery, then recomputes the two family levels (0–100). Exposes snapshot and breakdown for UI and telemetry.

### Key state (all 0–100 where applicable)

- **Family levels:** `negative`, `positive`.
- **Positive buckets:** `pos_rarity_pack`, `pos_streak`, `pos_economy`.
- **Negative buckets:** `neg_rarity_pack`, `neg_streak`, `neg_economy`.

### Main API

- `ApplyPackOutcome(string packTypeKey, List<string> rarities)` – full pipeline; returns `EmotionDeltaResult` (positive/negative deltas).
- `Snapshot()` – returns `(negative, positive)`.
- `GetLastNegativeDelta()`, `GetLastPositiveDelta()`, `GetLastBreakdown()` (Phase2PullBreakdown), `GetLastStreakLength()`, `GetLastRareBoostApplied()` – for UI/telemetry.
- `ResetSession()` – zeros all state and clears streak window.

### Pipeline (summary)

1. Raw score and max rarity from rarities; normalize to quality01 using pack score range.
2. Pack-type bias on quality01 (e.g. Bronze optimistic, Gold stricter).
3. General deltas dP, dN from asymmetric curves; optional rare-card boost on dP.
4. Apply bucket decay.
5. Route dP into pos*rarity_pack / pos_streak / pos_economy and dN into neg*\* by routing rules (quality thresholds, streak mood, value score).
6. Recovery: good pull reduces negative; bad pull reduces positive.
7. Recompute `positive` and `negative` from buckets (configurable weights).
8. Update rolling quality window for streak logic.

### Config

Uses `DropConfigManager.Instance.config`: `phase_2_configuration` (emotion_parameters, families, routing, decay, recovery), `pack_types`, `rarity_values`.

### Related types in same file

- `EmotionDeltaResult` (positive, negative).
- `Phase2PullBreakdown` – pack_type, raw_score, quality01, cost, streak/value flags, pos/neg emotion labels and per-bucket weights/deltas, applied totals, positive_after, negative_after.

---

## 11. EmotionDisplayUI

**File:** `Assets/Scripts/CCAS/EmotionDisplayUI.cs`

### What it does

Phase 2 emotion UI: two bars (Negative, Positive 0–100) with borders and labels, and short-lived popups for emotion labels (Thrill, Relief, Worth; Disappointment, Letdown, Regret) based on the last pull’s breakdown.

### Key members

- **Text:** `negativeText`, `positiveText`.
- **Bars:** `negativeBar`, `positiveBar` (RectTransforms), `negativeBorderImage`, `positiveBorderImage`.
- **Colors:** `negativeColor`, `positiveColor`.
- **Animation:** `lerpSpeed` for bar/label lerp.
- **Popups:** `popupTextPrefab`, `negativePopupAnchor`, `positivePopupAnchor`, `popupLifetimeSeconds`, `popupRisePixels`, `popupStackSpacing`.

### Flow

- `Start`: init bar anchors/pivots/sizes, read initial state from `EmotionalStateManager.Snapshot()`.
- `Update`: lerp displayed values toward current snapshot; update bar widths, label text/color/alpha, border intensity; call `TrySpawnPopups()`.
- `TrySpawnPopups()`: compare last breakdown to a simple event key; if new, spawn popups for non-zero bucket deltas (positive: Thrill/Relief/Worth; negative: Disappointment/Letdown/Regret). Popups fade and rise then are destroyed.
- `OnDisable`: clear event key and destroy popup clones.

### Dependencies

- `EmotionalStateManager.Instance` for `Snapshot()` and `GetLastBreakdown()`.

---

## 12. HookOrchestrator

**File:** `Assets/Scripts/CCAS/HookOrchestrator.cs`

### What it does

Singleton that gates “hooks” (e.g. outcome-based telemetry or effects) with a global quiet window, per-hook cooldowns, and per-session caps. Currently used to decide when an outcome hook (e.g. `outcome_streak`) is fired and logged.

### Key members

- `quietWindowRange` (Vector2: min/max seconds for global quiet after a fire).
- Internal: `_globalQuietUntil`, `_cooldownsUntil`, `_sessionCaps`.

### API

- `TryFireHook(hookId, cooldownSeconds, sessionCap, payloadAction, out blockReason)` – returns false if blocked by global quiet, session cap, or cooldown; otherwise runs payload, updates cooldown and cap, sets new quiet window, returns true.
- `TryTriggerOutcomeHooks(List<string> rarities)` – tries to fire `outcome_streak` (fixed cooldown 5s, cap 5); logs success/failure to `TelemetryLogger`.
- `ResetHooks()` – clears cooldowns and session caps.

### Notes

- No JSON config; values are hardcoded in the script.

---

## 13. PackOpeningController

**File:** `Assets/Scripts/CCAS/PackOpeningController.cs`

### What it does

Opens a pack by key: pulls cards from `DropConfigManager`, updates emotions and hooks, logs the pull to `TelemetryLogger`, and displays cards in the pack panel using `CardView` (or rarity-only fallback). Continue button goes to Drop History.

### Key members

- **Navigation:** `continueButton`, `packPanel`, `dropHistoryPanel`; `dropHistoryController`.
- **Cards:** `cardParent`, `cardPrefab`; `packType` (string key).

### Flow

- `OpenPackOfType(key)` sets `packType` and calls `OpenPack()`.
- `OpenPack()`: get `List<Card>` from `DropConfigManager.PullCards(packType)`; if empty, fall back to `PullCardRarities` and rarity-only display. Otherwise: build/reuse card instances, build `raritiesForHooks`, call `EmotionalStateManager.ApplyPackOutcome(packType, raritiesForHooks)`, `HookOrchestrator.TryTriggerOutcomeHooks(raritiesForHooks)`, `TelemetryLogger.LogPull(packType, packName, cost, cards)`, then set each card UI via `SetCard(go, card)` (uses `CardView.Apply(card)` when present). Continue button shows History and refreshes `DropHistoryController`.

### SetCard

- Overload with `Card`: rarity color on Image, then `CardView.Apply(card)` if present else primary TMP text = card name.
- Overload with string rarity: Image color and TMP text from `RarityColorUtility`.

---

## 14. PlayerWallet

**File:** `Assets/Scripts/CCAS/PlayerWallet.cs`

### What it does

Singleton managing player coins: affordability check, spend for pack, add coins. Persists to PlayerPrefs (`wallet_coins`); raises `OnChanged` when balance changes.

### API

- `CanAfford(PackType p)` – coins >= p.cost.
- `SpendForPack(PackType p)` – if can afford, deduct cost, save, invoke `OnChanged`, return true.
- `AddCoins(int amount)` – increase coins, save, `OnChanged`.
- Persistence: `SaveWallet()` / `LoadWallet()` (PlayerPrefs).

### Notes

- Starting balance from inspector or `PlayerPrefs.GetInt("wallet_coins", coins)`.

---

## 15. RarityColorUtility

**File:** `Assets/Scripts/CCAS/RarityColorUtility.cs`

### What it does

Static single source for rarity → color: common, uncommon, rare, epic, legendary to fixed Color32 values. Unknown/empty returns white.

### API

- `GetColorForRarity(string rarity)` – returns `Color32`. Input is case-normalized to lower.

Used by `CardView`, `PackOpeningController`, `DropHistoryController`, and any UI that colors by rarity.

---

## 16. TelemetryLogger

**File:** `Assets/Scripts/CCAS/TelemetryLogger.cs`

### What it does

Singleton that logs every pack pull to an in-memory list and persists to `pull_history.json`. Builds duplicate state from pull history, assigns XP for duplicates (Phase 1 Part 4), updates `player_xp` in PlayerPrefs, and exports Phase 2 emotional state to a CSV file. Fires `OnPullLogged` so Drop History can refresh.

### Key paths

- JSON: `Application.persistentDataPath/Telemetry/pull_history.json`
- CSV: `.../Telemetry/csv_exports/PHASE_2_EMOTIONAL_STATE_LOG.csv`  
  **Where this actually is (easy to miss):** Not in the repo; written at runtime. **macOS:** `~/Library/Application Support/DefaultCompany/MGI_Monorepo/Telemetry/pull_history.json`. **Windows:** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\pull_history.json`. See **CCAS_File_Map.md** → “Where telemetry is written” for full detail.

### Main API

- `LogPull(packTypeKey, packName, packCostCoins, List<Card> cards)` – full logging with card data, duplicate detection, XP, emotional snapshot and Phase 2 breakdown; append to cache, trim to MaxLogs, save file, export CSV, invoke `OnPullLogged`.
- `LogPull(..., List<string> rarities)` – legacy overload without card data.
- `LogHookExecution(hookId, fired, reasonIfBlocked, context)` – debug logging only.
- `GetRecent(int count)` – last N `PackPullLog` entries.
- `ClearLogFile()` – clear in-memory logs and save (Context menu).

### Duplicate and XP

- `BuildCardPullCountsFromHistory()` builds UID → pull count from cached logs (including previous pulls in same session).
- Duplicate = card UID already seen (previousPulls > 0). XP per duplicate from `GetDuplicateXpForRarity(rarity)` using `config.duplicate_xp` or fallback defaults.
- Player total XP: `PlayerPrefs.GetInt("player_xp", 0)` + total_xp_gained for the pull; saved back to PlayerPrefs.

### Log structures

- **PackPullLog:** event_id, timestamp, session_id, player_id, player_level, pack_type, pack_name, cost_coins, pull_results, pulled_cards (List<CardData>), positive_after, negative_after, phase2_breakdown, total_xp_gained, duplicate_count, player_xp_after.
- **CardData:** uid, name, team, element, rarity, position5, is_duplicate, xp_gained, total_pulls_for_card, duplicate_pulls_for_card.

### Dependencies

- `DropConfigManager` (duplicate_xp), `EmotionalStateManager` (snapshot + breakdown for CSV and log).

---

## Cross-reference

- **Config:** `phase2_config.json` → DropConfigManager + DropConfigModels.
- **Cards:** `cards_catalog.json` → CardCatalogLoader; Card, CardsCatalog.
- **Phase 2 design:** `Assets/Resources/CCAS/Phase2/Phase2_Emotional_System_Specification.md`.
- **Phase 1 design (emotion formula, duplicate conversion):** `Assets/Resources/CCAS/Phase1/`.
- **Quick script list:** `CCAS_Script_Reference_Overview.md` (this folder).
- **Workflow flowcharts (navigation, pack open, startup):** `CCAS_Workflow_Flowchart.md` (this folder).
- **Every CCAS file and folder:** `CCAS_File_Map.md` (this folder).
