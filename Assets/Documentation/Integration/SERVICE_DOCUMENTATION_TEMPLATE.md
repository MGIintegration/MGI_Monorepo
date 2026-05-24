# {ServiceName} — Service Overview

> **Template:** Copy this file, rename it (e.g. `Economy_SERVICE.md`), and replace every `{placeholder}`. Keep it to **one page or less**. Link to schemas or code only when needed — details live in code and `StreamingAssets`.

| | |
|---|---|
| **Team** | {TeamName} |
| **Service class** | `{ServiceName}.cs` |
| **Code path** | `Assets/Scripts/{Domain}/Backend/` |
| **Status** | Draft / In progress / Complete |

---

## Purpose

One or two sentences: what this service owns and what it does **not** own.

> Example: Central offline API for wallet balances and spend/add operations. Does not open packs or award XP directly.

---

## Public API

List methods other teams (or UI) should call. One line per method — behavior, not implementation.

| Method | Returns | Behavior |
|--------|---------|----------|
| `{MethodName}(playerId, …)` | `{Type}` | {When to use; success/failure semantics} |
| | | |

**Conventions**

- `playerId`: use the same id as other services (e.g. `"local_player"` in single-player).
- Missing player state: {create if missing / return null / throw — pick one}.
- Spending: prefer `{EconomyService.TrySpend}` pattern — fail closed, no partial writes.

---

## Data

### Config (read-only, shipped)

| File | Location |
|------|----------|
| `{config.json}` | `Assets/StreamingAssets/{Domain}/` |

### Runtime (read/write, per player)

| File | Resolved via |
|------|----------------|
| `{state.json}` | `FilePathResolver.Get{Domain}Path(playerId, fileName)` |

Schemas (if any): `Assets/StreamingAssets/{Domain}/*_schema.json`

**This service owns:** {list files only this team writes}

**This service reads but does not own:** {other teams’ runtime or config files}

---

## Dependencies

| Depends on | Why |
|------------|-----|
| `FilePathResolver` | All per-player JSON paths |
| `EventBus` | Publish/subscribe (if used) |
| `{OtherService}` | {e.g. spend before pack open} |

Other teams should **not** call private helpers or write this service’s JSON directly.

---

## Events

| Direction | `event_type` | When |
|-----------|--------------|------|
| **Publishes** | `{event_name}` | {trigger} |
| **Subscribes** | `{event_name}` | {reaction, or “none”} |

**Example payload** (opaque JSON in `EventEnvelope.payloadJson`):

```json
{
  "player_id": "local_player",
  "example_field": "value"
}
```

Idempotency: if consuming events, document whether `{service}` checks `processed_events.json` by `event_id`.

---

## Typical flow

Short numbered flow for the main cross-team action (3–5 steps max).

1. UI calls `{Service}.{Method}(playerId, …)`.
2. Service validates → calls `{Dependency}` if needed.
3. Persists state → publishes `{event}` (optional).
4. UI refreshes from `{Method}` or EventBus subscription.

---