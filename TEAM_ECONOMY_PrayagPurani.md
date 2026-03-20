# Team Economy - Prayag Purani

This file tracks Economy team implementation work done by Prayag Purani.

## Scope Covered

- HealthApi
- WalletSimulationApi
- EconomyService (Integration Plan item 1.2)

## EconomyService (1.2) - Implementation Guide

Goal: single entry point for all wallet operations.

### Required File

- `Assets/Scripts/Economy/Backend/EconomyService.cs`

### Required Public API

```csharp
Wallet GetWallet(string playerId, bool createIfMissing = true)
bool TrySpend(string playerId, int coins, int gems, string source, out Wallet updatedWallet)
void AddCurrency(string playerId, int coins, int gems, string source)
IEnumerable<WalletTransaction> GetRecentTransactions(string playerId, int limit)
```

### Step-by-Step Implementation

1) Define dependencies in `EconomyService`:
- `FilePathResolver` for paths
- `EventBus` for optional `wallet_updated` publish
- JSON serializer (`Newtonsoft.Json` or Unity JsonUtility wrapper used in project)

2) Resolve economy paths with `FilePathResolver`:
- Wallet file: `GetEconomyPath(playerId, "wallet.json")`
- Transactions file: `GetEconomyPath(playerId, "wallet_transactions.json")`

3) Implement `GetWallet`:
- If file exists, read and deserialize `Wallet`.
- If missing and `createIfMissing == true`, create default wallet:
  - `player_id = playerId`
  - `coins = 0`, `gems = 0`, `coaching_credits = 0`
  - `last_updated = UTC ISO-8601`
- Persist default wallet using atomic write.
- If missing and `createIfMissing == false`, return `null` (or project-approved equivalent behavior).

4) Implement `TrySpend`:
- Load wallet via `GetWallet(playerId, true)`.
- Validate non-negative spend input.
- Check affordability (`wallet.coins >= coins` and `wallet.gems >= gems`).
- If insufficient funds:
  - `updatedWallet = wallet`
  - return `false`
- If sufficient:
  - Deduct balances
  - Update `last_updated`
  - Create `WalletTransaction`:
    - `id = Guid.NewGuid().ToString()`
    - `player_id = playerId`
    - `type = "spend"`
    - `currency` and `amount` based on each non-zero deduction
    - `source = source`
    - `timestamp = UTC ISO-8601`
  - Save wallet + append transactions atomically
  - Optionally publish `wallet_updated`
  - set `updatedWallet`
  - return `true`

5) Implement `AddCurrency`:
- Load wallet via `GetWallet(playerId, true)`.
- Validate non-negative add input.
- Add coin/gem amounts.
- Update `last_updated`.
- Create and append `WalletTransaction` entries with `type = "earn"`.
- Persist with atomic write.
- Optionally publish `wallet_updated`.

6) Implement `GetRecentTransactions`:
- Read `wallet_transactions.json`.
- Filter by `player_id`.
- Sort descending by `timestamp`.
- Return top `limit`.

7) Atomic write strategy (required):
- Serialize JSON to temp file (`*.tmp`).
- Flush/write temp.
- Move/replace temp -> real path.
- Apply this for both wallet and transaction updates.

8) Event payload recommendation for `wallet_updated`:
- `player_id`
- `coins`
- `gems`
- `coaching_credits`
- `source`
- `timestamp`

### Validation Checklist

- [ ] `GetWallet` creates missing wallet correctly
- [ ] `TrySpend` fails cleanly on insufficient balance
- [ ] `TrySpend` writes transaction IDs as GUID
- [ ] `AddCurrency` updates balance and transaction history
- [ ] Transaction reads support limit and ordering
- [ ] All writes are atomic (temp + move)
- [ ] `wallet_updated` event publishes when enabled

## Work Log (Prayag)

### HealthApi

- Owner: Prayag Purani
- Status: In Progress
- Implemented endpoints: Unknown
- Request/response contract location: Unknown
- Data file dependencies: Unknown
- EventBus integration: Unknown
- Test status: Unknown
- Notes: Need to finalize endpoint list and schema mapping.

### WalletSimulationApi

- Owner: Prayag Purani
- Status: In Progress
- Implemented endpoints: Unknown
- Simulation rules source: Unknown
- EconomyService dependency: Expected
- Wallet transaction generation: Unknown
- EventBus integration (`wallet_updated`): Unknown
- Test status: Unknown
- Notes: Need to align simulation output with `wallet.json` and `wallet_transactions.json`.

## Open Items to Fill Later

- Exact `HealthApi` route list
- Exact `WalletSimulationApi` route list
- Source model/schema files used by both APIs
- Unit/integration test file paths
- Known limitations and edge cases

