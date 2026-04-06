# Team Member Documentation Template

> One document per team member. Use this template so onboarding/offboarding is smooth—new hires inherit a file, departing members hand off theirs.

---

## Owner Overview

| Field | Value |
|-------|-------|
| **Owner** | Your name |
| **Team** | Team name |
| **Last Updated** | |

---

## 1. Code Ownership & File-Level Notes

### 1.1 Module Map

| Module/File | Purpose | Entry Points |
|-------------|---------|--------------|
| `Path/To/File.cs` | Brief description | Main(), Initialize(), etc. |

### 1.2 Key Flows

| Flow | Trigger | Steps | Exit/Handoff |
|------|---------|-------|--------------|
| Example: Acquisition pack opening | User action | 1. Validate → 2. Award → 3. Emit event | XP/compensation to PlayerService |

### 1.3 Dependencies

- **Internal:** Modules/scripts my work depends on
- **External:** Third-party or Unity packages
- **Cross-team:** Teams and modules I consume from

### 1.4 Gotchas

- Non-obvious behavior, race conditions, or edge cases
- Known technical debt or temporary workarounds

---

## 2. Schemas & Contracts

### 2.1 JSON Schemas

| Schema Name | Version | Location | Description |
|-------------|---------|----------|-------------|
| Example: `PackOpenResult` | v1 | `Assets/.../schemas/` | Shape of pack open response |

### 2.2 Payload Shapes

- **Request:** Field list, types, required vs optional
- **Response:** Field list, types, error shapes
- **Events:** Event names, payload structure

### 2.3 Validation Rules

- Constraints (min/max, formats, enums)
- Validation layers (client, server, both)

### 2.4 Versioning Assumptions

- How schema changes are versioned
- Breaking vs non-breaking change policy

---

## 3. Workflows / Pipelines

### 3.1 End-to-End Flow

```
Trigger → Step 1 → Step 2 → Step 3 → Outcome
         ↓ failure? → Retry / Fallback / Alert
```

### 3.2 Failure Handling

| Failure Type | Behavior | Retries | Escalation |
|--------------|----------|---------|------------|
| Network timeout | | Yes/No, count | Log / Alert / Notify |
| Validation fail | | N/A | Return error to caller |

### 3.3 Monitoring & Logging

- Log levels and what gets logged
- Metrics/events emitted
- Where to look for debugging (logs, dashboards)

---

## 4. Integration Points

### 4.1 What I Emit (to other teams)

| Event/Payload | Consumer Team(s) | Contract Location | Trigger |
|---------------|------------------|-------------------|---------|
| Example: `PlayerXPAwarded` | PlayerService, Analytics | `docs/contracts/` | Pack open complete |

### 4.2 What I Consume (from other teams)

| Event/Payload | Producer Team | Contract Location | When Used |
|---------------|---------------|-------------------|-----------|
| Example: `InventoryUpdated` | Inventory | `docs/contracts/` | Refresh UI |

### 4.3 Contract Locations

- Links or paths to shared contract docs (DocMost, repo, etc.)

---

## 5. Tunable Values & Rationale

### 5.1 Inventory (for my area)

| Variable | Default | Location | Rationale | Impact of Change |
|----------|---------|----------|-----------|------------------|
| XP per acquisition | 50 | `Config/XPRewards.json` | Balances progression speed | Faster/slower leveling |
| Duplicate player XP drop | 25% | Same | Incentivizes variety | More/less duplicate value |
| Duplicate coin compensation | 100 | Same | Fairness threshold | Economy balance |
| Duplicate gem compensation | 5 | Same | Premium duplicate handling | Monetization sensitivity |
| XP cap per session | 500 | Env / DB | Anti-abuse | Exploit resistance vs UX |

### 5.2 Location Legend

- **Config:** JSON/YAML in repo
- **File:** Hardcoded or asset
- **Env:** Environment variable
- **DB:** Database / remote config

### 5.3 Change Impact Summary

- **Capabilities:** What features depend on each value
- **Trade-offs:** What degrades if you increase/decrease

---

## 6. Documentation Status

| Section | Status |
|---------|--------|
| Code ownership | ☐ Done |
| Schemas/contracts | ☐ Done |
| Workflows | ☐ Done |
| Integration (emit) | ☐ Done |
| Integration (consume) | ☐ Done |
| Tunable values | ☐ Done |

**Handoff notes:** Context, open questions, or pointers for whoever takes over.

---

## Quick Links

- [Workflow Examples (Streamlit)](https://mgi-system-diagram.streamlit.app/)
- [DocMost - Standardized Doc](link TBD)
- [Contract Repository](path TBD)

---

## Appendix: Template Usage

1. **One file per team member** — e.g. `TEAM_Acquisition_JohnDoe.md` or `Acquisition_JohnDoe.md`
2. New hire: copy template → rename → fill in your areas
3. Offboarding: hand off your file to successor; they inherit and update
4. Align to the integration team's finalized standardized doc once posted
5. Update on meaningful changes; keep "Last Updated" current
