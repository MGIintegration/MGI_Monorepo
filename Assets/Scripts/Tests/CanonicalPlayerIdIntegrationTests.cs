#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CCAS.Backend;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Verifies that a non-default active profile remains the same player across
/// CCAS, Economy, Facilities, Coaches, and Progression.
/// </summary>
public static class CanonicalPlayerIdIntegrationTests
{
    private const string TestPlayerId = "__canonical_player_id_test__";
    private const string OtherPlayerId = "__canonical_player_id_other__";
    private const string TestTeamId = "canonical-player-id-team";
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = new List<string>();

    public static void RunAll()
    {
        bool passed = RunSuite().Passed;
        if (Application.isBatchMode)
            EditorApplication.Exit(passed ? 0 : 1);
    }

    public static TestSuiteRunResult RunSuite()
    {
        _passed = 0;
        _failed = 0;
        Failures.Clear();

        bool hadPreviousPlayer = PlayerPrefs.HasKey(PlayerIdProvider.PlayerPrefsKey);
        string previousPlayer = PlayerPrefs.GetString(PlayerIdProvider.PlayerPrefsKey);
        Debug.Log("===== CanonicalPlayerIdIntegrationTests: starting =====");

        try
        {
            PlayerIdProvider.Set(TestPlayerId);
            AssertEqual(TestPlayerId, PlayerIdProvider.Get(), "Provider: resolves the active player id");

            var ccas = GetOrCreateCCASService();
            var progression = GetOrCreateProgressionService();
            var dropConfig = GetOrCreateDropConfigManager();
            var catalog = GetOrCreateCardCatalogLoader();
            var facilities = new FacilitiesService();

            Assert(dropConfig.config != null, "Setup: CCAS pack configuration is loaded");
            Assert(catalog.catalog?.cards?.Length > 0, "Setup: CCAS card catalog is loaded");
            ResetState(ccas, progression, facilities, TestPlayerId);
            ResetState(ccas, progression, facilities, OtherPlayerId);
            new EconomyService().AddCurrency(TestPlayerId, 2_000_000, 0, 0, "canonical_player_id_test_seed");

            // The test runner can retain a destroyed singleton from an earlier
            // suite. Re-register the live loaders before exercising CCAS.
            DropConfigManager.Instance = dropConfig;
            CardCatalogLoader.Instance = catalog;

            Test_CcasAndEconomyUseActiveProfile(ccas);
            Test_FacilitiesUseActiveProfile(facilities);
            Test_CoachesUseActiveProfile();
            Test_ProgressionUsesActiveProfile(progression);
        }
        catch (Exception exception)
        {
            Fail("Unhandled test exception: " + exception);
        }
        finally
        {
            CleanupTestState();
            if (hadPreviousPlayer)
                PlayerIdProvider.Set(previousPlayer);
            else
            {
                PlayerPrefs.DeleteKey(PlayerIdProvider.PlayerPrefsKey);
                PlayerPrefs.Save();
            }
        }

        Debug.Log($"===== CanonicalPlayerIdIntegrationTests: {_passed} passed, {_failed} failed =====");
        if (_failed > 0)
            Debug.Log("FAILURES:\n - " + string.Join("\n - ", Failures));
        return new TestSuiteRunResult(_passed, _failed, Failures);
    }

    private static void Test_CcasAndEconomyUseActiveProfile(CCASService ccas)
    {
        var economy = new EconomyService();
        int coinsBefore = economy.GetWallet(TestPlayerId).coins;
        var result = ccas.OpenPack(null, "bronze_pack");

        Assert(result != null && result.success, "CCAS: pack opens without an explicit player id");
        Assert(ccas.GetCollection(TestPlayerId).Any(), "CCAS: cards save under the active player id");
        Assert(economy.GetWallet(TestPlayerId).coins < coinsBefore,
            "Economy: pack debit applies to the active player id");
        Assert(!ccas.GetCollection(OtherPlayerId).Any(), "CCAS: another player profile remains isolated");
    }

    private static void Test_FacilitiesUseActiveProfile(FacilitiesService facilities)
    {
        AssertEqual(TestPlayerId, facilities.GetPlayerFacilityState(null).player_id,
            "Facilities: omitted player id resolves to the active profile");

        bool upgraded = facilities.TryUpgradeFacility(PlayerIdProvider.Get(), "film_room", out var progress);
        Assert(upgraded && progress != null && progress.level == 2,
            "Facilities: upgrade uses the active player id");
    }

    private static void Test_CoachesUseActiveProfile()
    {
        var coach = CoachesService.GetAvailableCoaches()
            .FirstOrDefault(record => record?.coach_type == "O");
        Assert(coach != null, "Coaches: an offense coach is available for the active profile");
        if (coach == null)
            return;

        Assert(CoachesService.TryHireCoach(TestTeamId, coach.coach_id, out _, null),
            "Coaches: default hire uses the active player id");
        AssertEqual(TestPlayerId, CoachesService.GetTeamState()?.player_id,
            "Coaches: team state saves under the active player id");
    }

    private static void Test_ProgressionUsesActiveProfile(ProgressionService progression)
    {
        progression.AddXp(PlayerIdProvider.Get(), 7, "canonical_player_id_test", "canonical-player-id-event");
        var state = progression.GetState(TestPlayerId, createIfMissing: false);
        Assert(state != null && state.current_xp >= 7f,
            "Progression: XP is stored under the active player id");
    }

    private static void ResetState(CCASService ccas, ProgressionService progression, FacilitiesService facilities, string playerId)
    {
        ccas.ResetPlayerState(playerId);
        progression.ClearPlayerProgression(playerId);
        facilities.ResetFacilityState(playerId);
        CoachesService.ResetPlayerCoachState(playerId);
        new EconomyService().ResetWallet(playerId);
    }

    private static void CleanupTestState()
    {
        var ccas = CCASService.Instance;
        var progression = ProgressionService.Instance;
        var facilities = new FacilitiesService();
        foreach (string playerId in new[] { TestPlayerId, OtherPlayerId })
        {
            ccas?.ResetPlayerState(playerId);
            progression?.ClearPlayerProgression(playerId);
            facilities.ResetFacilityState(playerId);
            CoachesService.ResetPlayerCoachState(playerId);
            new EconomyService().ResetWallet(playerId);
        }
    }

    private static CCASService GetOrCreateCCASService()
    {
        return CCASService.Instance != null
            ? CCASService.Instance
            : new GameObject("CanonicalPlayerId_CCASSvc").AddComponent<CCASService>();
    }

    private static ProgressionService GetOrCreateProgressionService()
    {
        return ProgressionService.Instance != null
            ? ProgressionService.Instance
            : new GameObject("CanonicalPlayerId_ProgressionSvc").AddComponent<ProgressionService>();
    }

    private static DropConfigManager GetOrCreateDropConfigManager()
    {
        var manager = DropConfigManager.Instance != null
            ? DropConfigManager.Instance
            : new GameObject("CanonicalPlayerId_DropConfig").AddComponent<DropConfigManager>();
        if (manager.config == null)
            typeof(DropConfigManager).GetMethod("LoadConfig", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(manager, null);
        return manager;
    }

    private static CardCatalogLoader GetOrCreateCardCatalogLoader()
    {
        var loader = CardCatalogLoader.Instance != null
            ? CardCatalogLoader.Instance
            : new GameObject("CanonicalPlayerId_CardCatalog").AddComponent<CardCatalogLoader>();
        if (loader.catalog == null)
            typeof(CardCatalogLoader).GetMethod("LoadCatalog", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(loader, null);
        return loader;
    }

    private static void Assert(bool condition, string description)
    {
        if (condition)
        {
            _passed++;
            Debug.Log("[CanonicalPlayerIdIntegrationTests] PASS: " + description);
            return;
        }

        _failed++;
        Failures.Add(description);
        Debug.LogError("[CanonicalPlayerIdIntegrationTests] FAIL: " + description);
    }

    private static void AssertEqual(string expected, string actual, string description)
    {
        Assert(string.Equals(expected, actual, StringComparison.Ordinal),
            $"{description} (expected '{expected}', got '{actual}')");
    }

    private static void Fail(string description) => Assert(false, description);
}
#endif
