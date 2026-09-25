# MGI test diagnostic report

- Generated (UTC): 2026-09-21T19:17:37.9642710Z
- Unity version: 6000.3.2f1
- Selection: `all`
- Overall result: **PASS**

## Full-run results

| Suite | Subsystem | Result | Scope |
| --- | --- | --- | --- |
| `progression` | Progression and LocalSeasonBackend | **PASS** (46 passed, 0 failed) | XP state, tiers, idempotency, history, season rewards, and week simulation. |
| `progression-facilities` | Facilities -> Progression and Economy | **PASS** (19 passed, 0 failed) | Facility XP multipliers, upgrades, and insufficient-funds gating. |
| `progression-ccas` | CCAS -> Progression and Economy | **PASS** (24 passed, 0 failed) | Pack purchase cost gating, duplicate-card XP, and event idempotency. |
| `ccas` | CCAS service | **PASS** (36 passed, 0 failed) | Pack validation, persistence, duplicate XP, refunds, reset, seed, and telemetry cleanup. |
| `ccas-progression` | CCAS -> Progression | **PASS** (15 passed, 0 failed) | Duplicate-card XP persistence, history, events, player isolation, and funds gating. |
| `ccas-economy` | CCAS -> Economy | **PASS** (14 passed, 0 failed) | Pack cost deduction, transaction ledger, collection/history persistence, events, and funds gating. |
| `ccas-boundary` | CCAS -> Progression; Facilities and Coaches boundary | **PASS** (17 passed, 0 failed) | Duplicate-card XP remains owned by CCAS despite facility upgrades and active coaches. |

## Test inventory and subsystem connections

| Test surface | Connection | Execution type |
| --- | --- | --- |
| `ProgressionUnitTests` | ProgressionService, LocalSeasonBackend, FacilitiesService | Automated Unity batch suite |
| `ProgressionUnitTests.RunFacilitiesIntegration` | FacilitiesService -> ProgressionService and EconomyService | Automated Unity batch suite |
| `ProgressionUnitTests.RunCcasIntegration` | CCASService -> ProgressionService and EconomyService | Automated Unity batch suite |
| `CCASUnitTests` | CCASService, EconomyService, ProgressionService, FacilitiesService, CoachesService, EventBus, catalog/config loaders | Automated Unity batch suite |
| `CCASProgressionIntegrationTests` | CCASService -> ProgressionService | Automated Unity integration suite |
| `CCASEconomyIntegrationTests` | CCASService -> EconomyService | Automated Unity integration suite |
| `CCASFacilitiesCoachesBoundaryIntegrationTests` | CCAS duplicate XP boundary; FacilitiesService and CoachesService must not modify it | Automated Unity integration suite |
| `EconomyServiceSmokeTestRunner` | EconomyService wallet, currency, spending, transactions, and events | Manual Play Mode/context-menu diagnostic; does not fail the process |
| `EconomyServiceJsonOperationsTester` | Economy JSON files | Manual inspector utility |
| `SystemTester` | SaveLoadLogic, RuntimeValidator, StatusDeltaChecker, CoachManager | Manual coroutine/scene diagnostic |
| `QuickTestPanel` | Coaches save/load, validation, delta checks, and singleton presence | Manual UI diagnostic |
| `CoachPreloadTester` | CoachManager preload/reload/fire behavior | Manual keyboard/UI diagnostic |
| `test_PackOpeningController`, `test_BoosterMarketAuto`, `test_AcquisitionHubController` | Older CCAS UI prototypes | Historical/commented-out, not executable |

## Engine-independent assessment

The current automated suites cannot be moved wholesale to a normal .NET unit-test project without changing their coverage: they instantiate Unity `GameObject`/`MonoBehaviour` services, invoke Unity lifecycle methods, load Unity-backed configuration, use `Application.persistentDataPath`, publish through Unity-connected services, and exercise file-backed state.

The practical extraction candidates are pure progression tier calculation, XP/idempotency rules, economy balance/transaction rules, duplicate-XP lookup, pack/drop calculations, and configuration parsing. Those rules are currently embedded in the Unity services/configuration boundary, so this run keeps them as Unity tests rather than creating a second test implementation that could drift. A future refactor can move those calculations behind plain C# interfaces and add a normal .NET test project without weakening the integration coverage above.

## Reproduction

Full run:
```text
Unity -batchmode -projectPath <path-to-MGI_Monorepo> -executeMethod TestSuiteRunner.RunAll -quit -logFile <path-to-log>
```

Individual selection:
```text
MGI_TESTS=ccas-economy Unity -batchmode -projectPath <path-to-MGI_Monorepo> -executeMethod TestSuiteRunner.RunSelected -quit -logFile <path-to-log>
```

Available selections: `progression`, `progression-facilities`, `progression-ccas`, `ccas`, `ccas-progression`, `ccas-economy`, `ccas-boundary`, or `all`.
