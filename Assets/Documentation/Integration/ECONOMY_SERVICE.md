# EconomyService — Service Overview

| | |
|---|---|
| **Team** | Economy |
| **Service class** | `EconomyService.cs` |
| **Code path** | `Assets/Scripts/Economy/Backend/` |
| **Status** | Complete |

---

## Purpose

Central offline API for player wallet balances, currency grants, spending, and wallet transaction history. It owns wallet runtime JSON only; it does not open packs, award XP, register contracts, calculate salaries, or apply facility upgrades directly.

---

## Public API

| Method | Returns | Behavior |
|--------|---------|----------|
| `GetWallet(playerId, createIfMissing)` | `Wallet` | Reads the wallet. Creates a default wallet when `createIfMissing` is true; returns `null` when creation is off and state is missing/invalid. |
| `TrySpend(playerId, coins, gems, source)` | `bool` | Deducts coins/gems only when affordable. Returns `false` with no mutation on insufficient funds. |
| `TrySpend(playerId, coins, gems, source, out wallet)` | `bool` | Same spend flow, also returns the updated wallet. |
| `TrySpend(playerId, coins, gems, credits, source, out wallet)` | `bool` | Same spend flow including `coaching_credits`. |
| `AddCurrency(playerId, coins, gems, source)` | `void` | Adds coins/gems, writes transactions, and publishes a wallet update. Zero/negative input is ignored. |
| `AddCurrency(playerId, coins, gems, credits, source)` | `void` | Same add flow including `coaching_credits`. |
| `GetRecentTransactions(playerId, limit)` | `IEnumerable<WalletTransaction>` | Returns the newest transactions for the player. |

**Conventions**

- `playerId`: use the same id as other services; single-player tests use `"local_player"`.
- Missing player state: `GetWallet(..., createIfMissing: true)` creates it; `createIfMissing: false` returns `null`.
- Spending: use `EconomyService.TrySpend`; failed spends fail closed with no partial writes and no transaction append.
- Transaction sources should be meaningful domain strings such as `pack_purchase`, `coach_hiring`, `upgrade_facility`, or salary-deduction sources. Blank sources normalize to `unknown_source`.
- API-facing enum mapping: `currency: 0 = coins`; `operation: 0 = add`; `operation: 1 = spend`.

---

## Data

### Config (read-only, shipped)

| File | Location |
|------|----------|
| `wallet_schema.json` | `Assets/StreamingAssets/Economy/` |
| `wallet_transactions_schema.json` | `Assets/StreamingAssets/Economy/` |
| `economy_forecast.json` | `Assets/StreamingAssets/Economy/` |

### Runtime (read/write, per player)

| File | Resolved via |
|------|--------------|
| `wallet.json` | `FilePathResolver.GetEconomyPath(playerId, "wallet.json")` |
| `wallet_transactions.json` | `FilePathResolver.GetEconomyPath(playerId, "wallet_transactions.json")` |

Schemas: `Assets/StreamingAssets/Economy/*_schema.json`

Runtime path: `Application.persistentDataPath/mgi_state/{playerId}/economy/`

**This service owns:** `wallet.json`, `wallet_transactions.json`

**This service reads but does not own:** event log files via `EventBus`, forecast data via `EconomyForecastService`, and caller-owned flows from `PackOpeningController`, `FacilitiesService`, `WalletApi`, and `SalaryEngineApi`.

---

## Dependencies

| Depends on | Why |
|------------|-----|
| `FilePathResolver` | All per-player economy JSON paths. |
| `EventBus` | Publish `wallet_updated` after wallet changes. |
| `EconomyForecastService` | Forecast sync after wallet changes. |
| `Newtonsoft.Json` | JSON read/write. |
| `WalletApi` | API wallet display/update and insufficient-funds handling. |
| `SalaryEngineApi` | Salary deductions update wallet balances through economy spend behavior. |

Other teams should **not** call private helpers or write `wallet.json` / `wallet_transactions.json` directly.

---

## Events

| Direction | `event_type` | When |
|-----------|--------------|------|
| **Publishes** | `wallet_updated` | After every successful `TrySpend` or `AddCurrency`. |
| **Subscribes** | none | Not implemented. |

**Example payload** (opaque JSON in `EventEnvelope.payloadJson`):

```json
{
  "player_id": "local_player",
  "coins": 775,
  "gems": 40,
  "coaching_credits": 10,
  "source": "coach_hiring",
  "timestamp": "2026-05-18T00:00:00.0000000Z"
}
```

Idempotency: `EconomyService` does not currently consume events, so it does not check `processed_events.json` by `event_id`.

---

## Typical flow

1. UI or another service calls `EconomyService.TrySpend(playerId, coins, gems, source, out wallet)`.
2. Service validates the wallet and amount; callers such as pack opening, salary deduction, and facility upgrade continue only after success.
3. Service persists wallet state and appends one `WalletTransaction` per changed currency.
4. Service publishes `wallet_updated`; UI refreshes from `GetWallet` or an `EventBus` subscription.
5. Facility upgrades must use source `upgrade_facility` before applying the level upgrade.
