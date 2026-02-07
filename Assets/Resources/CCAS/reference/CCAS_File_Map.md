# CCAS — Complete File Map

**Purpose:** This document lists **every** CCAS-related file and folder in the project so anyone can find where things live and what they are for. The whole CCAS feature was implemented by one person; this map is the handoff reference.

**Root locations:** CCAS content lives in five places under the repo:

1. **Assets/Scripts/CCAS/** — C# scripts (game logic, UI, config loading)
2. **Assets/StreamingAssets/CCAS/** — Runtime JSON config and data (loaded at runtime by the app)
3. **Assets/Resources/CCAS/** — Design docs, specs, reference docs, sample data (read by humans or loaded via `Resources.Load` if needed)
4. **Assets/Prefabs/CCAS/** — Unity prefabs used by the CCAS scene
5. **Assets/Scenes/CCAS/** — The main CCAS Unity scene

---

## 1. Assets/Scripts/CCAS/

**What it is:** All C# code for the Card Collection / Acquisition System (CCAS): navigation, pack opening, emotions, telemetry, config, cards.

**What each script does:** See **CCAS_Script_Reference_Overview.md** (Scripts at a Glance) and **CCAS_Scripts_Detailed.md** (full detail). This map only lists what exists and where.

| File | Type |
|------|------|
| AcquisitionHubController.cs | MonoBehaviour |
| BoosterMarketAuto.cs | MonoBehaviour |
| Card.cs | Data classes (Card, CardsCatalog) |
| CardCatalogLoader.cs | Singleton MonoBehaviour |
| CardView.cs | MonoBehaviour |
| DropConfigManager.cs | Singleton MonoBehaviour |
| DropConfigModels.cs | Data classes (CCAS.Config namespace) |
| DropHistoryController.cs | MonoBehaviour |
| EmotionalStateManager.cs | Singleton MonoBehaviour |
| EmotionDisplayUI.cs | MonoBehaviour |
| HookOrchestrator.cs | Singleton MonoBehaviour |
| PackOpeningController.cs | MonoBehaviour |
| PlayerWallet.cs | Singleton MonoBehaviour |
| RarityColorUtility.cs | Static class |
| TelemetryLogger.cs | Singleton MonoBehaviour |
| config/CCASConfigLoader.cs | Static class |

*Each `.cs` has a matching `.cs.meta` (Unity); ignore for “what it does.”*

---

## 2. Assets/StreamingAssets/CCAS/

**What it is:** Data files that are **copied as-is** into the built app and read at runtime. Path in builds: `Application.streamingAssetsPath/CCAS/`.

**Used for:** Pack types, drop rates, rarity values, emotion tuning, and the full card catalog. **Do not** put secrets here; these files are readable in the built app.

| File | What it is | Used for |
|------|------------|----------|
| **phase2_config.json** | JSON config | **Primary runtime config.** Pack types (name, cost, guaranteed_cards, drop_rates, score_range), rarity_values, Phase 2 emotion (families, routing, decay, recovery), duplicate_xp. Loaded by **DropConfigManager** on startup. Defines bronze/silver/gold (or other) packs and all emotion tuning. |
| **phase1_config.json** | JSON config | **Legacy.** Phase 1 schema (satisfaction/frustration, emotion_dynamics). Kept for reference or fallback. **DropConfigManager** loads **phase2_config.json** only; phase1 is not loaded by current code. |
| **cards_catalog.json** | JSON array of cards | **Card catalog.** Each object: `uid`, `cardTier` (1–5), `name`, `team`, `element`, `position5`. Loaded by **CardCatalogLoader** on startup. Used to pick actual cards when opening packs (by tier/rarity). |

*Each has a `.meta`; ignore for “what it does.”*

---

## 3. Assets/Resources/CCAS/

**What it is:** Design documents, specifications, reference docs, and any assets you might load via `Resources.Load("CCAS/...")`. Shipped with the build if under `Resources`.

**Used for:** Onboarding, design context, and (for CSV) possible import/source for catalog data. Most files here are for **humans**, not runtime logic.

### 3.1 Root of Resources/CCAS

| Path / File | What it is | Used for |
|-------------|------------|----------|
| **Assets/Resources/CCAS/Cards.10.csv** | CSV | Sample/source card data (many columns: UID, CardTier, Name, Team, Element, Position5, stats, etc.). **Not** loaded by code at runtime. Source or reference for generating or validating `StreamingAssets/CCAS/cards_catalog.json`. |

### 3.2 Resources/CCAS/Phase1/

**What it is:** Phase 1 design documents (emotion formula, duplicate conversion). Not loaded at runtime.

| File | What it is | Used for |
|------|------------|----------|
| **Emotion Formula Simplification_part1.pdf** | PDF | Design doc: emotion formula simplification (Part 1). Reference for how the emotion system was designed. |
| **Emotion Formula Simplification_part2.pdf** | PDF | Design doc: Part 2 (formula). Reference. |
| **Emotion Formula Simplification_part2_UPDATED.md** | Markdown | Updated written spec for Phase 1 Part 2 emotion (balanced edition, pipeline steps). Reference for emotion logic. |
| **Phase 1 – Part 4_ Duplicate Conversion System.pdf** | PDF | Design doc: duplicate conversion (XP for duplicate cards). Reference for duplicate/XP behavior. |

### 3.3 Resources/CCAS/Phase2/

**What it is:** Phase 2 specs and design (emotion system, pack types). Not loaded at runtime.

| File | What it is | Used for |
|------|------------|----------|
| **Phase2_Emotional_System_Specification.md** | Markdown | **Main Phase 2 spec:** emotion families (positive/negative), buckets (rarity_pack, streak, economy), routing, decay, recovery, config shape. Read this to understand Phase 2 emotion design and how it matches `phase2_config.json` and `EmotionalStateManager` / `EmotionDisplayUI`. |
| **Phase2_CardPacksandTypes.csv** | CSV | **Pack types and drop rates (5 packs).** Two sections in one file: (1) **Drop rates** — columns Pack Type, Common, Uncommon, Rare, Epic, Legendary (percentages per pack: Bronze 75/15/10/0/0, Silver 0/65/20/10/5, Gold 0/0/60/30/10, Elite 0/0/70/15/15, Supreme 0/0/0/50/50); (2) **Best and worst outcomes** — Pack Type, Worst, Best, Total cards (all 3 cards per pack). Use this when adding Elite and Supreme to `phase2_config.json` (see **CCAS_Future_Plan.md**). Not loaded at runtime. |
| **Phase2_CardPacksandTypes.xlsx** | Excel | Same content as the CSV; may be the source spreadsheet. Use **Phase2_CardPacksandTypes.csv** in the repo for reference or tooling. |

### 3.4 Resources/CCAS/reference/

**What it is:** Handoff and onboarding docs so new team members can work on CCAS without prior context.

| File | What it is | Used for |
|------|------------|----------|
| **CCAS_Script_Reference_Overview.md** | Markdown | Quick lookup: short description of each script, where to start, key dependencies, file locations. |
| **CCAS_Scripts_Detailed.md** | Markdown | Detailed per-script reference: what each script does, key members, flow, API, dependencies. |
| **CCAS_File_Map.md** | Markdown | **This file.** Complete list of every CCAS file and folder and what each is used for. |
| **CCAS_Workflow_Flowchart.md** | Markdown | Workflow flowcharts (Mermaid): user/UI navigation, pack-open pipeline, startup loading, data flow. Use for high-level understanding. |
| **CCAS_Future_Plan.md** | Markdown | Future plan and handoff: goals, next steps (5 pack types from Vrushali’s sheet, cards per pack), duplicate/emotion idea and mindset. Conversational guide for the next person. |

---

## 4. Assets/Prefabs/CCAS/

**What it is:** Unity prefabs used in the CCAS scene (UI and objects).

**Used for:** Consistent card display, pack buttons, and emotion popups across the flow.

| File | What it is | Used for |
|------|------------|----------|
| **Card.prefab** | Prefab | Single card slot in the pack-opening view. Typically has (or is used with) **CardView** and Image/TextMeshPro for rarity color and card name/team/element/position. Referenced by **PackOpeningController** as `cardPrefab`; instantiated under `cardParent`. |
| **BuyPackButton.prefab** | Prefab | Button for one pack type in the market. Used by **BoosterMarketAuto**: instantiated once per pack in config; label shows pack name and cost; click opens that pack. Assigned as `packButtonPrefab`; instantiated under `contentParent`. |
| **popups.prefab** | Prefab | Text popup for emotion labels (e.g. “Thrill”, “Relief”, “Disappointment”). Used by **EmotionDisplayUI** as `popupTextPrefab`; instantiated under negative/positive popup anchors, then faded and destroyed after a short time. |

---

## 5. Assets/Scenes/CCAS/

**What it is:** The Unity scene that runs the full CCAS flow.

| File | What it is | Used for |
|------|------------|----------|
| **CCAS.unity** | Unity scene | Main CCAS scene: hub, market, pack opening, drop history panels; references to Prefabs/CCAS prefabs and Scripts/CCAS components. Opening this scene is how you run and test CCAS. |

---

## Quick reference: “Where do I find…?”

| You need… | Look here |
|------------|-----------|
| Script that does X | **CCAS_Script_Reference_Overview.md** → then **CCAS_Scripts_Detailed.md** |
| **Workflow / flow (how it all fits together)** | **CCAS_Workflow_Flowchart.md** (Mermaid flowcharts) |
| **Future plan (next steps, 5 packs, duplicate/emotion)** | **CCAS_Future_Plan.md** |
| Every CCAS file and folder | **This file (CCAS_File_Map.md)** |
| Pack types, drop rates, emotion tuning | **Assets/StreamingAssets/CCAS/phase2_config.json** (loaded by DropConfigManager) |
| List of cards at runtime (uid, name, tier, etc.) | **Assets/StreamingAssets/CCAS/cards_catalog.json** (loaded by CardCatalogLoader) |
| **Card source/sample data (CSV)** | **Assets/Resources/CCAS/Cards.10.csv** (not loaded at runtime; source for catalog or reference) |
| **Pack types & drop rates (5 packs)** | **Assets/Resources/CCAS/Phase2/Phase2_CardPacksandTypes.csv** (drop rates + best/worst outcomes; see **CCAS_Future_Plan.md**) |
| Phase 2 emotion design | **Assets/Resources/CCAS/Phase2/Phase2_Emotional_System_Specification.md** |
| Phase 1 emotion / duplicate design | **Assets/Resources/CCAS/Phase1/** (PDFs and Emotion Formula Simplification_part2_UPDATED.md) |
| Card slot / pack button / popup visuals | **Assets/Prefabs/CCAS/** (Card, BuyPackButton, popups) |
| Run the feature | **Assets/Scenes/CCAS/CCAS.unity** |
| **Telemetry / pull_history.json (runtime, outside repo)** | **See “Where telemetry is written” below.** macOS: `~/Library/Application Support/DefaultCompany/MGI_Monorepo/Telemetry/`. Windows: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\`. Not in the project—easy to miss. |

---

## Runtime vs design-only

- **Loaded by the game at runtime:**  
  `phase2_config.json`, `cards_catalog.json` (StreamingAssets).  
  Telemetry writes to **Application.persistentDataPath** (e.g. `pull_history.json`, CSV exports), not under the CCAS folder in the repo.
- **Not loaded by code (design/reference):**  
  Everything under **Assets/Resources/CCAS/** (Phase1, Phase2, reference, and root files like `Cards.10.csv`) is for humans or as source data unless you add `Resources.Load` later.  
  `Cards.10.csv` is reference/source data.  
  **StreamingAssets:** `phase1_config.json` is not loaded by current code (phase2_config is used).

---

## Where telemetry is written (easy to miss)

**TelemetryLogger** writes at runtime to a folder **outside the project** so the files persist between runs. They’re not in the repo and can be hard to find.

- **Path in code:** `Application.persistentDataPath/Telemetry/`
  - **pull_history.json** — full pull log (events, cards, duplicates, XP, emotion snapshot).
  - **csv_exports/PHASE_2_EMOTIONAL_STATE_LOG.csv** — Phase 2 emotion rows appended per pull.
- **Examples (Unity default Company Name / Product Name):**
  - **macOS:** **`~/Library/Application Support/DefaultCompany/MGI_Monorepo/Telemetry/pull_history.json`** (and **Telemetry/csv_exports/** for the CSV).
  - **Windows:** **`%USERPROFILE%\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\pull_history.json`** (e.g. `C:\Users\YourName\AppData\LocalLow\DefaultCompany\MGI_Monorepo\Telemetry\`; **Telemetry\csv_exports\** for the CSV).
  Replace `DefaultCompany` / `MGI_Monorepo` if your **Player Settings → Company Name** or **Product Name** differ.
- **Why document this:** So testers and the next person can open the file to inspect logs, clear data, or copy it for analysis without hunting for it.

Keeping this file map updated when you add or move CCAS files will keep the handoff clear for everyone.
