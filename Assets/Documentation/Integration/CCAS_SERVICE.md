# CCAS — Service Overview

| | |
|---|---|
| **Team** | CCAS (Acquisition / Packs & Cards) |
| **Service class** | `CCASService.cs` |
| **Code path** | `Assets/Scripts/CCAS/Backend/` |
| **Status** | Complete (Phase 2 pack flow) |

---

## Purpose

Central offline API for **pack opening**, **card collection**, and **pack drop history**. Coordinates spend, card rolls, duplicate XP, persistence, and `buy_pack` events.

Does **not** own wallet balances, tier/XP config, or raw drop-table logic — those live in `EconomyService`, `ProgressionService`, and `DropConfigManager` respectively.

---

## Public API

| Method | Returns | Behavior |
|--------|---------|----------|
| `OpenPack(playerId, packTypeId)` | `PackResult` | Resolves pack cost from config → `EconomyService.TrySpend` (coins only, `source: "pack_purchase"`) → rolls cards via `DropConfigManager.PullCards` → detects duplicates, awards duplicate XP, updates collection & history → publishes `buy_pack`. On failure: `success = false`, `failureReason` set; **no** collection/history writes. |
| `GetCollection(playerId)` | `IEnumerable<CardCollectionEntry>` | Reads persisted `card_collection.json` for the player (empty if missing). |

**`PackResult` failure reasons:** `insufficient_funds` · `pack_not_found` · `catalog_error` · `invalid_player`

**Conventions**

- `playerId`: same id as Economy/Progression (UI often uses `PlayerPrefs["player_id"]` or `SystemInfo.deviceUniqueIdentifier`).
- Missing collection/history files: treated as empty; created on first successful open.
- Spending: fail closed via `EconomyService.TrySpend` before any roll or persistence.
- Duplicate XP: `ProgressionService.AddXp(..., source: "duplicate_card_{rarity}", eventId: "ccas_pack_open:{packOpenId}:{cardId}")` for idempotency.
- Scene: requires `CCASService.Instance` and `DropConfigManager.Instance` (both `DontDestroyOnLoad` singletons).

---

## Data

### Config (read-only, shipped)

| File | Location |
|------|----------|
| `phase2_config.json` | `Assets/StreamingAssets/CCAS/` — pack types, costs, duplicate XP rules (`duplicate_xp`) |
| `cards_catalog.json` | `Assets/StreamingAssets/CCAS/` — card definitions (used by `DropConfigManager`) |
| `phase1_config.json` | Legacy; superseded by `phase2_config.json` for runtime loads |

### Runtime (read/write, per player)

| File | Resolved via |
|------|----------------|
| `card_collection.json` | `FilePathResolver.GetCCASPath(playerId, "card_collection.json")` |
| `pack_drop_history.json` | `FilePathResolver.GetCCASPath(playerId, "pack_drop_history.json")` |

Schemas: `Assets/StreamingAssets/CCAS/card_collection_schema.json`, `pack_drop_history_schema.json`

**This service owns:** `card_collection.json`, `pack_drop_history.json` (under `{persistentDataPath}/mgi_state/{playerId}/ccas/`)

**This service reads but does not own:** `phase2_config.json`, `cards_catalog.json`; wallet/progression files (via other services only)

---

## Dependencies

| Depends on | Why |
|------------|-----|
| `FilePathResolver` | Per-player CCAS JSON paths |
| `EventBus` | Publish `buy_pack` after successful open |
| `EconomyService` | Deduct pack cost before roll (`TrySpend`) |
| `ProgressionService` | Award duplicate-card XP (`AddXp`) |
| `DropConfigManager` | Pack types, `PullCards`, duplicate XP config |

Other teams must **not** write CCAS JSON directly or bypass `OpenPack` for purchases.

---

## Events

| Direction | `event_type` | When |
|-----------|--------------|------|
| **Publishes** | `buy_pack` | After successful open (post-persist) |
| **Subscribes** | — | None |

**Example payload** (`EventEnvelope.payloadJson`):

```json
{
  "pack_type_id": "bronze_pack",
  "cost_paid": { "coins": 1000, "gems": 0 },
  "cards_pulled": [
    { "card_id": "card_001", "rarity": "common", "is_duplicate": false, "xp_awarded": 0 },
    { "card_id": "card_002", "rarity": "rare", "is_duplicate": true, "xp_awarded": 25 }
  ]
}
```

Idempotency: CCAS does not consume events. Duplicate XP uses explicit `eventId` on `ProgressionService.AddXp` so replays do not double-award.

---

## Typical flow

1. UI (`PackOpeningController`, `BoosterMarketAuto`) calls `CCASService.OpenPack(playerId, packTypeId)`.
2. Service spends coins via `EconomyService`; on failure, UI shows insufficient funds.
3. Rolls cards, updates collection quantities, appends `PackDropHistoryEntry`, awards duplicate XP via `ProgressionService`.
4. Publishes `buy_pack`; UI renders `PackResult.cards` (no second spend/event from UI when service path is used).
