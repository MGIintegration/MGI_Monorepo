#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Lightweight, dependency-free unit test harness for Progression's services.
/// Runs against the real ProgressionService/LocalSeasonBackend (no mocking framework
/// available in this project - there are no assembly definitions yet, so a proper
/// Unity Test Framework test assembly can't reference this code). Invoke via:
///   Unity.exe -batchmode -projectPath <path> -executeMethod ProgressionUnitTests.RunAll -quit -logFile <path>
/// Exits the Editor process with a non-zero code if any test fails, for CI/script use.
/// </summary>
public static class ProgressionUnitTests
{
    private const string TestPlayerId = "__unit_test_progression__";

    private static int _passed;
    private static int _failed;
    private static readonly List<string> _failures = new List<string>();

    public static void RunAll()
    {
        _passed = 0;
        _failed = 0;
        _failures.Clear();

        Log("===== ProgressionUnitTests: starting =====");

        var progression = GetOrCreateProgressionService();
        var backend = GetOrCreateLocalSeasonBackend();

        // Always start from a clean slate for the test player.
        progression.ClearPlayerProgression(TestPlayerId);

        Test_NewPlayer_StartsAtZeroRookie(progression);
        Test_AddXp_IncreasesTotal(progression);
        Test_TierBoundaries_MatchConfig(progression);
        Test_AddXp_NonPositiveIsNoOp(progression);
        Test_AddXp_NullOrEmptyPlayerIdIsSafe(progression);
        Test_AddXp_DuplicateEventIdIsIdempotent(progression);
        Test_XpHistory_RecordsSourceAndAmount(progression);
        Test_GetAllTiers_MatchesConfig(progression);
        Test_ClearPlayerProgression_ResetsState(progression);

        // CreateSeason runs first, matching real gameplay order: a season must be
        // created (which warms up LocalSeasonBackend's ProgressionService reference)
        // before any match simulation or season-end rewards can occur.
        Test_CreateSeason_InitializesPlayerProgression(backend, progression);
        Test_AwardSeasonRewards_PlacementXp(backend, progression);
        Test_SimulateWeek_AwardsMatchXpAndAdvancesWeek(backend, progression);
        Test_SimulateWeek_RejectsPastFinalWeek(backend);

        // Leave no residue in mgi_state for the test player.
        progression.ClearPlayerProgression(TestPlayerId);

        Log($"===== ProgressionUnitTests: {_passed} passed, {_failed} failed =====");
        if (_failed > 0)
        {
            Log("FAILURES:\n - " + string.Join("\n - ", _failures));
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(_failed > 0 ? 1 : 0);
        }
    }

    // ---------------- ProgressionService ----------------

    private static void Test_NewPlayer_StartsAtZeroRookie(ProgressionService progression)
    {
        var state = progression.GetState(TestPlayerId, createIfMissing: true);
        AssertEqual(0, state.current_xp, "NewPlayer_StartsAtZeroRookie: current_xp");
        AssertEqual("rookie", state.current_tier, "NewPlayer_StartsAtZeroRookie: current_tier");
    }

    private static void Test_AddXp_IncreasesTotal(ProgressionService progression)
    {
        progression.ClearPlayerProgression(TestPlayerId);
        progression.AddXp(TestPlayerId, 10, "unit_test", Guid.NewGuid().ToString());
        var state = progression.GetState(TestPlayerId, createIfMissing: false);
        AssertEqual(10, state.current_xp, "AddXp_IncreasesTotal");
    }

    private static void Test_TierBoundaries_MatchConfig(ProgressionService progression)
    {
        progression.ClearPlayerProgression(TestPlayerId);

        // Walk up to exactly 49 -> still rookie
        progression.AddXp(TestPlayerId, 49, "unit_test", Guid.NewGuid().ToString());
        AssertEqual("rookie", progression.GetState(TestPlayerId).current_tier, "TierBoundaries: 49 XP is rookie");

        // Cross into pro at 50
        progression.AddXp(TestPlayerId, 1, "unit_test", Guid.NewGuid().ToString());
        AssertEqual("pro", progression.GetState(TestPlayerId).current_tier, "TierBoundaries: 50 XP is pro");

        // Up to 99 -> still pro
        progression.AddXp(TestPlayerId, 49, "unit_test", Guid.NewGuid().ToString());
        AssertEqual("pro", progression.GetState(TestPlayerId).current_tier, "TierBoundaries: 99 XP is pro");

        // Cross into all_star at 100
        progression.AddXp(TestPlayerId, 1, "unit_test", Guid.NewGuid().ToString());
        AssertEqual("all_star", progression.GetState(TestPlayerId).current_tier, "TierBoundaries: 100 XP is all_star");

        // Up to 149 -> still all_star
        progression.AddXp(TestPlayerId, 49, "unit_test", Guid.NewGuid().ToString());
        AssertEqual("all_star", progression.GetState(TestPlayerId).current_tier, "TierBoundaries: 149 XP is all_star");

        // Cross into legend at 150
        progression.AddXp(TestPlayerId, 1, "unit_test", Guid.NewGuid().ToString());
        AssertEqual("legend", progression.GetState(TestPlayerId).current_tier, "TierBoundaries: 150 XP is legend");
    }

    private static void Test_AddXp_NonPositiveIsNoOp(ProgressionService progression)
    {
        progression.ClearPlayerProgression(TestPlayerId);
        progression.AddXp(TestPlayerId, 20, "unit_test", Guid.NewGuid().ToString());
        int before = progression.GetState(TestPlayerId).current_xp;

        progression.AddXp(TestPlayerId, 0, "unit_test", Guid.NewGuid().ToString());
        progression.AddXp(TestPlayerId, -5, "unit_test", Guid.NewGuid().ToString());

        int after = progression.GetState(TestPlayerId).current_xp;
        AssertEqual(before, after, "AddXp_NonPositiveIsNoOp");
    }

    private static void Test_AddXp_NullOrEmptyPlayerIdIsSafe(ProgressionService progression)
    {
        try
        {
            progression.AddXp(null, 10, "unit_test", Guid.NewGuid().ToString());
            progression.AddXp("", 10, "unit_test", Guid.NewGuid().ToString());
            Assert(true, "AddXp_NullOrEmptyPlayerIdIsSafe: no exception thrown");
        }
        catch (Exception ex)
        {
            Assert(false, $"AddXp_NullOrEmptyPlayerIdIsSafe: threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Test_AddXp_DuplicateEventIdIsIdempotent(ProgressionService progression)
    {
        progression.ClearPlayerProgression(TestPlayerId);
        string eventId = Guid.NewGuid().ToString();

        progression.AddXp(TestPlayerId, 15, "unit_test", eventId);
        progression.AddXp(TestPlayerId, 15, "unit_test", eventId); // duplicate grant, same eventId

        AssertEqual(15, progression.GetState(TestPlayerId).current_xp, "AddXp_DuplicateEventIdIsIdempotent");
    }

    private static void Test_XpHistory_RecordsSourceAndAmount(ProgressionService progression)
    {
        progression.ClearPlayerProgression(TestPlayerId);
        progression.AddXp(TestPlayerId, 7, "match_win", Guid.NewGuid().ToString());

        var state = progression.GetState(TestPlayerId);
        var entry = state.xp_history.LastOrDefault();

        Assert(entry != null, "XpHistory_RecordsSourceAndAmount: entry exists");
        if (entry != null)
        {
            AssertEqual(7, entry.xp_gained, "XpHistory_RecordsSourceAndAmount: xp_gained");
            AssertEqual("match_win", entry.source, "XpHistory_RecordsSourceAndAmount: source");
        }
    }

    private static void Test_GetAllTiers_MatchesConfig(ProgressionService progression)
    {
        var tiers = progression.GetAllTiers();
        AssertEqual(4, tiers.Count, "GetAllTiers_MatchesConfig: tier count");

        AssertTierMatches(tiers, "rookie", 0, 49);
        AssertTierMatches(tiers, "pro", 50, 99);
        AssertTierMatches(tiers, "all_star", 100, 149);
        AssertTierMatches(tiers, "legend", 150, 999);
    }

    private static void AssertTierMatches(Dictionary<string, TierData> tiers, string key, int expectedMin, int expectedMax)
    {
        if (!tiers.TryGetValue(key, out var tier))
        {
            Assert(false, $"GetAllTiers_MatchesConfig: '{key}' tier missing");
            return;
        }

        AssertEqual(expectedMin, tier.min_xp, $"GetAllTiers_MatchesConfig: {key} min_xp");
        AssertEqual(expectedMax, tier.max_xp, $"GetAllTiers_MatchesConfig: {key} max_xp");
    }

    private static void Test_ClearPlayerProgression_ResetsState(ProgressionService progression)
    {
        progression.AddXp(TestPlayerId, 30, "unit_test", Guid.NewGuid().ToString());
        progression.ClearPlayerProgression(TestPlayerId);

        var stateAfterClear = progression.GetState(TestPlayerId, createIfMissing: false);
        Assert(stateAfterClear == null, "ClearPlayerProgression_ResetsState: state absent after clear");

        var freshState = progression.GetState(TestPlayerId, createIfMissing: true);
        AssertEqual(0, freshState.current_xp, "ClearPlayerProgression_ResetsState: fresh state is 0 XP");
    }

    // ---------------- LocalSeasonBackend ----------------

    private static void Test_AwardSeasonRewards_PlacementXp(LocalSeasonBackend backend, ProgressionService progression)
    {
        AssertPlacementReward(backend, progression, placement: 1, baseXp: 50);
        AssertPlacementReward(backend, progression, placement: 2, baseXp: 30);
        AssertPlacementReward(backend, progression, placement: 3, baseXp: 15);
        AssertPlacementReward(backend, progression, placement: 4, baseXp: 0);
    }

    private static void AssertPlacementReward(LocalSeasonBackend backend, ProgressionService progression, int placement, int baseXp)
    {
        progression.ClearPlayerProgression(TestPlayerId);

        var season = BuildSeasonWithPlayerAtPlacement(placement);
        string result = null;
        string error = null;
        backend.AwardSeasonRewards(season, s => result = s, e => error = e);

        Assert(error == null, $"AwardSeasonRewards_PlacementXp: placement {placement} did not error ({error})");

        // AddXp applies Facilities' "season_reward" multiplier on top of the base reward
        // (this is the intended Facility-affected-XP behavior, not a bug) - compute the
        // expected value the same way production does rather than assuming a 1:1 mapping.
        int expectedXp = baseXp == 0
            ? 0
            : Mathf.Max(1, Mathf.RoundToInt(baseXp * new FacilitiesService().GetProgressionXpMultiplier(TestPlayerId, "season_reward")));

        int actualXp = progression.GetState(TestPlayerId, createIfMissing: false)?.current_xp ?? 0;
        AssertEqual(expectedXp, actualXp, $"AwardSeasonRewards_PlacementXp: placement {placement} awards {expectedXp} XP (base {baseXp})");
    }

    private static SeasonSaveData BuildSeasonWithPlayerAtPlacement(int placement)
    {
        // 4 teams, points descending 300/200/100/0. Player team's points position
        // determines its placement (1st..4th) among all teams.
        var pointsByRank = new[] { 300, 200, 100, 0 };

        var season = new SeasonSaveData
        {
            season_id = Guid.NewGuid().ToString(),
            current_week = 5,
            total_weeks = 10,
            teams = new List<TeamSaveData>()
        };

        for (int rank = 1; rank <= 4; rank++)
        {
            bool isPlayer = rank == placement;
            season.teams.Add(new TeamSaveData
            {
                team_id = Guid.NewGuid().ToString(),
                player_id = isPlayer ? TestPlayerId : Guid.NewGuid().ToString(),
                team_name = isPlayer ? "PlayerTeam" : $"AiTeam{rank}",
                rating = 1000,
                is_player_team = isPlayer,
                stats = new TeamStatsSaveData { points = pointsByRank[rank - 1] }
            });
        }

        return season;
    }

    private static void Test_CreateSeason_InitializesPlayerProgression(LocalSeasonBackend backend, ProgressionService progression)
    {
        SeasonSaveData created = null;
        string error = null;
        backend.CreateSeason(new List<string> { "Jets", "Hawks" }, "MyTeam", s => created = s, e => error = e);

        Assert(error == null, $"CreateSeason_InitializesPlayerProgression: no error ({error})");
        Assert(created != null, "CreateSeason_InitializesPlayerProgression: season returned");
        if (created != null)
        {
            AssertEqual(3, created.teams.Count, "CreateSeason_InitializesPlayerProgression: team count (2 AI + player)");

            var playerTeam = created.teams.FirstOrDefault(t => t.is_player_team);
            Assert(playerTeam != null, "CreateSeason_InitializesPlayerProgression: player team present");
            if (playerTeam != null)
            {
                var state = progression.GetState(playerTeam.player_id, createIfMissing: false);
                Assert(state != null, "CreateSeason_InitializesPlayerProgression: progression state created for player");
                if (state != null)
                {
                    AssertEqual(0, state.current_xp, "CreateSeason_InitializesPlayerProgression: starts at 0 XP");
                }

                // NOTE: CreateSeason hardcodes player_id = "local_player" (LocalSeasonBackend.cs:117),
                // not the canonical device-id convention the DM testing doc calls for. Cleaning up
                // whatever state this created so the test doesn't leave real "local_player" residue.
                progression.ClearPlayerProgression(playerTeam.player_id);
            }
        }
    }

    private static void Test_SimulateWeek_AwardsMatchXpAndAdvancesWeek(LocalSeasonBackend backend, ProgressionService progression)
    {
        progression.ClearPlayerProgression(TestPlayerId);

        var season = new SeasonSaveData
        {
            season_id = Guid.NewGuid().ToString(),
            current_week = 0,
            total_weeks = 10,
            teams = new List<TeamSaveData>
            {
                new TeamSaveData
                {
                    team_id = Guid.NewGuid().ToString(),
                    player_id = TestPlayerId,
                    team_name = "PlayerTeam",
                    is_player_team = true,
                    stats = new TeamStatsSaveData()
                },
                new TeamSaveData
                {
                    team_id = Guid.NewGuid().ToString(),
                    player_id = Guid.NewGuid().ToString(),
                    team_name = "AiTeam",
                    is_player_team = false,
                    stats = new TeamStatsSaveData()
                }
            }
        };

        SeasonSaveData updated = null;
        string error = null;
        backend.SimulateWeek(season, s => updated = s, e => error = e);

        Assert(error == null, $"SimulateWeek_AwardsMatchXpAndAdvancesWeek: no error ({error})");
        Assert(updated != null, "SimulateWeek_AwardsMatchXpAndAdvancesWeek: season returned");
        if (updated != null)
        {
            AssertEqual(1, updated.current_week, "SimulateWeek_AwardsMatchXpAndAdvancesWeek: week advanced to 1");
        }

        int xpGained = progression.GetState(TestPlayerId, createIfMissing: false)?.current_xp ?? 0;
        // Match played (5) + win (10) = 15, or match played (5) + loss (2) = 7 - outcome is randomized.
        Assert(xpGained == 15 || xpGained == 7,
            $"SimulateWeek_AwardsMatchXpAndAdvancesWeek: XP is 15 (win) or 7 (loss), got {xpGained}");
    }

    private static void Test_SimulateWeek_RejectsPastFinalWeek(LocalSeasonBackend backend)
    {
        var season = new SeasonSaveData
        {
            season_id = Guid.NewGuid().ToString(),
            current_week = 10,
            total_weeks = 10,
            teams = new List<TeamSaveData>()
        };

        SeasonSaveData updated = null;
        string error = null;
        backend.SimulateWeek(season, s => updated = s, e => error = e);

        Assert(updated == null, "SimulateWeek_RejectsPastFinalWeek: no success callback fired");
        Assert(error != null, "SimulateWeek_RejectsPastFinalWeek: error callback fired");
    }

    // ---------------- Test infrastructure ----------------

    // In batchmode edit-mode (no Play Mode session), Unity does not reliably invoke
    // Awake() synchronously after AddComponent within a single -executeMethod call.
    // Real gameplay always runs Awake() before anything else touches these singletons,
    // so we force it here to match that guarantee rather than silently testing against
    // uninitialized singletons. Awake() on these types calls DontDestroyOnLoad, which
    // Unity forbids outside Play Mode and throws for - that exception aborts the rest
    // of Awake() (e.g. config loading), so we swallow it here and let callers finish
    // any remaining setup (see GetOrCreateProgressionService).
    private static void ForceAwake(MonoBehaviour target)
    {
        var method = target.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            method?.Invoke(target, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            // Expected: "DontDestroyOnLoad can only be used in play mode" - Instance
            // assignment (which happens before the DontDestroyOnLoad call in these
            // Awake() implementations) has already taken effect by this point.
        }
    }

    private static void ForcePrivateMethod(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(target, null);
    }

    private static ProgressionService GetOrCreateProgressionService()
    {
        var existing = ProgressionService.Instance;
        if (existing != null) return existing;

        var go = new GameObject("ProgressionService_TestHarness");
        var service = go.AddComponent<ProgressionService>();
        ForceAwake(service);
        // Awake() aborted before reaching LoadProgressionConfig() - finish it explicitly.
        ForcePrivateMethod(service, "LoadProgressionConfig");
        return service;
    }

    private static LocalSeasonBackend GetOrCreateLocalSeasonBackend()
    {
        var existing = LocalSeasonBackend.Instance;
        // LocalSeasonBackend.Instance's own getter already creates+returns one via
        // AddComponent, but Awake() may not have run synchronously (see ForceAwake).
        // Nothing follows DontDestroyOnLoad in its Awake(), so no further setup needed.
        ForceAwake(existing);
        return existing;
    }

    private static void Assert(bool condition, string testName)
    {
        if (condition)
        {
            _passed++;
            Log($"PASS: {testName}");
        }
        else
        {
            _failed++;
            _failures.Add(testName);
            Log($"FAIL: {testName}");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string testName)
    {
        bool equal = EqualityComparer<T>.Default.Equals(expected, actual);
        Assert(equal, equal ? testName : $"{testName} (expected {expected}, got {actual})");
    }

    private static void Log(string message)
    {
        Debug.Log($"[ProgressionUnitTests] {message}");
    }
}
#endif
