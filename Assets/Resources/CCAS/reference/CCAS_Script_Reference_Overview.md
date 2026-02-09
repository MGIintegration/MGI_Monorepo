# CCAS Script Reference — Quick Overview

Short description of each script in **Assets/Scripts/CCAS** so you can find the right one. For full detail per script, see **CCAS_Scripts_Detailed.md**. For every CCAS file/folder and where it lives, see **CCAS_File_Map.md**. For workflow flowcharts, see **CCAS_Workflow_Flowchart.md**. For future plan and next steps (5 pack types, cards per pack, duplicate/emotion mindset), see **CCAS_Future_Plan.md**.

---

## Where to Start (scripts)

- **Navigation / flow:** `AcquisitionHubController` → `BoosterMarketAuto` → `PackOpeningController` → `DropHistoryController`
- **Data / config:** `DropConfigManager`, `DropConfigModels`, `CCASConfigLoader`, `CardCatalogLoader`, `Card`, `CardsCatalog`
- **Emotions (Phase 2):** `EmotionalStateManager`, `EmotionDisplayUI`
- **Economy / persistence:** `PlayerWallet`, `TelemetryLogger`
- **Utilities:** `RarityColorUtility`, `CardView`, `HookOrchestrator`

---

## Scripts at a Glance

| Script | Purpose |
|--------|--------|
| **AcquisitionHubController** | Central navigation: shows one panel at a time (Hub, Market, Pack Opening, Drop History, My Packs). Updates coins/XP on hub. |
| **BoosterMarketAuto** | Builds the booster market UI from config: one button per pack type. Clicking a pack opens it via the hub. |
| **Card** | Data model for a single card (uid, tier, name, team, element, position5). Converts tier ↔ rarity string. |
| **CardsCatalog** | Root class for the cards catalog JSON (array of `Card`). |
| **CardCatalogLoader** | Singleton. Loads `cards_catalog.json` from StreamingAssets/CCAS, indexes by tier, provides `GetRandomCardByTier` / `GetRandomCardByRarity`. |
| **CardView** | Renders one card slot: applies rarity color, name/team/element/position, optional fade-in. Used by pack opening. |
| **CCASConfigLoader** | Static helper to load any JSON config from `StreamingAssets/CCAS/` by filename. |
| **DropConfigManager** | Singleton. Loads `phase2_config.json`, defines pack types and drop rates. `PullCardRarities` / `PullCards` do weighted rolls and return rarities or full `Card` list. |
| **DropConfigModels** | Data classes for config: `CCASConfigRoot`, Phase 2 (emotion families, routing, decay, recovery), `PackType`, `DropRates`, `RarityValue`, `DuplicateXP`, etc. |
| **DropHistoryController** | Drop History panel: shows recent pulls (card names, rarity colors, duplicate + XP). Fixed labels for latest Positive/Negative (Phase 2). Refreshes on open and when `TelemetryLogger` fires `OnPullLogged`. |
| **EmotionalStateManager** | Singleton. Phase 2 emotion engine: computes positive/negative deltas from pack outcome, routes into buckets (rarity_pack, streak, economy), applies decay and recovery. Exposes `ApplyPackOutcome`, `Snapshot`, and breakdown for UI/telemetry. |
| **EmotionDisplayUI** | Phase 2 UI: two bars (Negative / Positive 0–100), labels, and emotion popups (Thrill, Relief, Worth; Disappointment, Letdown, Regret) from last pull breakdown. |
| **HookOrchestrator** | Singleton. Controls when outcome/hook logic can run: cooldowns, session caps, global quiet window. Currently used for `outcome_streak` hook logged to telemetry. |
| **PackOpeningController** | Opens a pack by key: pulls cards via `DropConfigManager`, updates emotions and hooks, logs to `TelemetryLogger`, displays cards with `CardView` (or fallback rarity-only). Continue → History. |
| **PlayerWallet** | Singleton. Coins only; `CanAfford` / `SpendForPack` / `AddCoins`. Persists to PlayerPrefs; fires `OnChanged`. |
| **RarityColorUtility** | Static mapping: rarity string → `Color32` (common → legendary). Single source for all rarity colors. |
| **TelemetryLogger** | Singleton. Logs each pack pull (event, cards, duplicates, XP, emotional snapshot). Persists to `pull_history.json`; exports Phase 2 emotional state to CSV. `OnPullLogged` notifies Drop History. |

---

## Key Dependencies

- **Config:** `phase2_config.json` (StreamingAssets/CCAS) → `DropConfigManager` + `DropConfigModels`
- **Cards (runtime):** `cards_catalog.json` (StreamingAssets/CCAS) → `CardCatalogLoader`; card prefabs use `CardView` and `RarityColorUtility`
- **Cards (source/sample):** `Assets/Resources/CCAS/Cards.10.csv` — CSV card data; not loaded at runtime; used as source/reference for the catalog (see **CCAS_File_Map.md**)
- **Phase 2 spec:** `Assets/Resources/CCAS/Phase2/Phase2_Emotional_System_Specification.md`
- **Phase 1 design docs (emotion formula, duplicate conversion):** `Assets/Resources/CCAS/Phase1/` — PDFs and Emotion Formula Simplification_part2_UPDATED.md (see **CCAS_File_Map.md**)
- **Telemetry / pull_history.json (runtime, outside repo):** Written by **TelemetryLogger**; not in the project. **macOS:** `~/Library/Application Support/DefaultCompany/MGI_Monorepo/Telemetry/pull_history.json`. **Windows:** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\pull_history.json`. Full detail: **CCAS_File_Map.md** → “Where telemetry is written.”

**File and folder locations:** only in **CCAS_File_Map.md** (single place for “where is X?”).
