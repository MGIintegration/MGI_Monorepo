#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CCAS.Backend;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Repeatable CCAS -> Progression workflow checks using real file-backed services.
/// Run with:
/// Unity -batchmode -projectPath <path> -executeMethod CCASProgressionIntegrationTests.RunAll -quit -logFile <path>
/// </summary>
public static class CCASProgressionIntegrationTests
{
    private const string TestPlayerId = "__integration_test_ccas_progression__";
    private const string OtherPlayerId = "__integration_test_ccas_progression_other__";
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = new List<string>();

    public static void RunAll()
    {
        _passed = 0;
        _failed = 0;
        Failures.Clear();
        Log("===== CCASProgressionIntegrationTests: starting =====");

        var ccas = GetOrCreateCCASService();
        var dropConfig = GetOrCreateDropConfigManager();
        var catalog = GetOrCreateCardCatalogLoader();
        var progression = GetOrCreateProgressionService();

        Assert(dropConfig.config != null, "Setup: CCAS pack configuration loaded");
        Assert(catalog.catalog?.cards?.Length > 0, "Setup: CCAS card catalog loaded");

        Test_DuplicatePack_AwardsAndPersistsProgressionXp(ccas, dropConfig, catalog, progression);
        Test_InsufficientFunds_DoesNotAwardProgressionXp(ccas, progression);

        ResetWorkflowState(ccas, progression);
        Log($"===== CCASProgressionIntegrationTests: {_passed} passed, {_failed} failed =====");
        if (_failed > 0)
            Log("FAILURES:\n - " + string.Join("\n - ", Failures));

        if (Application.isBatchMode)
            EditorApplication.Exit(_failed > 0 ? 1 : 0);
    }

    private static void Test_DuplicatePack_AwardsAndPersistsProgressionXp(
        CCASService ccas,
        DropConfigManager dropConfig,
        CardCatalogLoader catalog,
        ProgressionService progression)
    {
        ResetWorkflowState(ccas, progression);
        const string packTypeId = "bronze_pack";
        var pack = dropConfig.config.pack_types[packTypeId];
        var seededCollection = catalog.catalog.cards
            .Where(card => card != null && !string.IsNullOrWhiteSpace(card.uid))
            .Select(card => new CardCollectionEntry { card_id = card.uid, quantity = 1 })
            .ToList();

        Assert(ccas.SeedCollectionForTesting(TestPlayerId, seededCollection),
            "DuplicatePack: CCAS seeds a known collection");
        new EconomyService().AddCurrency(TestPlayerId, pack.cost, 0, 0, "integration_test_seed");

        int xpUpdatedEventCount = 0;
        using (EventBus.Subscribe("xp_updated", evt =>
        {
            if (evt.player_id == TestPlayerId)
                xpUpdatedEventCount++;
        }))
        {
            var result = ccas.OpenPack(TestPlayerId, packTypeId);
            int duplicateCount = result.cardDetails.Count(detail => detail.isDuplicate);
            int expectedXp = result.cardDetails.Sum(detail => detail.xpAwarded);
            var state = progression.GetState(TestPlayerId, createIfMissing: false);
            var otherPlayerState = progression.GetState(OtherPlayerId, createIfMissing: false);

            Assert(result.success, "DuplicatePack: CCAS opens the pack");
            AssertEqual(pack.guaranteed_cards, duplicateCount,
                "DuplicatePack: seeded cards are all detected as duplicates");
            Assert(expectedXp > 0, "DuplicatePack: CCAS calculates positive duplicate XP");
            AssertEqual(expectedXp, state?.current_xp ?? 0,
                "DuplicatePack: Progression receives exactly CCAS duplicate XP");
            AssertEqual(duplicateCount, state?.xp_history?.Count ?? 0,
                "DuplicatePack: Progression persists one XP history entry per duplicate");
            Assert(state != null && state.xp_history.All(entry =>
                    entry.source != null && entry.source.StartsWith("duplicate_card_")),
                "DuplicatePack: XP history identifies CCAS duplicate-card sources");
            AssertEqual(expectedXp, state?.xp_history?.Sum(entry => entry.xp_gained) ?? 0,
                "DuplicatePack: persisted XP history totals the awarded amount");
            AssertEqual(duplicateCount, xpUpdatedEventCount,
                "DuplicatePack: Progression publishes one XP update per duplicate");
            AssertEqual(0, otherPlayerState?.current_xp ?? 0,
                "DuplicatePack: another player receives no XP");
        }
    }

    private static void Test_InsufficientFunds_DoesNotAwardProgressionXp(CCASService ccas, ProgressionService progression)
    {
        ResetWorkflowState(ccas, progression);
        var result = ccas.OpenPack(TestPlayerId, "bronze_pack");
        var state = progression.GetState(TestPlayerId, createIfMissing: false);

        Assert(!result.success && result.failureReason == "insufficient_funds",
            "InsufficientFunds: CCAS rejects the purchase");
        AssertEqual(0, state?.current_xp ?? 0,
            "InsufficientFunds: Progression XP remains unchanged");
        AssertEqual(0, state?.xp_history?.Count ?? 0,
            "InsufficientFunds: Progression XP history remains empty");
    }

    private static void ResetWorkflowState(CCASService ccas, ProgressionService progression)
    {
        ccas.ResetPlayerState(TestPlayerId);
        progression.ClearPlayerProgression(TestPlayerId);
        progression.ClearPlayerProgression(OtherPlayerId);
        new EconomyService().ResetWallet(TestPlayerId);
        new FacilitiesService().ResetFacilityState(TestPlayerId);
        CoachesService.ResetPlayerCoachState(TestPlayerId);
    }

    private static CCASService GetOrCreateCCASService()
    {
        if (CCASService.Instance != null)
            return CCASService.Instance;

        var service = new GameObject("CCASProgressionIntegration_CCASSvc").AddComponent<CCASService>();
        ForceAwake(service);
        return service;
    }

    private static DropConfigManager GetOrCreateDropConfigManager()
    {
        if (DropConfigManager.Instance != null)
            return DropConfigManager.Instance;

        var manager = new GameObject("CCASProgressionIntegration_DropConfig").AddComponent<DropConfigManager>();
        ForceAwake(manager);
        ForcePrivateMethod(manager, "LoadConfig");
        return manager;
    }

    private static CardCatalogLoader GetOrCreateCardCatalogLoader()
    {
        if (CardCatalogLoader.Instance != null)
            return CardCatalogLoader.Instance;

        var loader = new GameObject("CCASProgressionIntegration_CardCatalog").AddComponent<CardCatalogLoader>();
        ForceAwake(loader);
        ForcePrivateMethod(loader, "LoadCatalog");
        return loader;
    }

    private static ProgressionService GetOrCreateProgressionService()
    {
        if (ProgressionService.Instance != null)
            return ProgressionService.Instance;

        var service = new GameObject("CCASProgressionIntegration_ProgressionSvc").AddComponent<ProgressionService>();
        ForceAwake(service);
        ForcePrivateMethod(service, "LoadProgressionConfig");
        return service;
    }

    private static void ForceAwake(MonoBehaviour target)
    {
        var method = target.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            method?.Invoke(target, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            // DontDestroyOnLoad is not valid in edit-mode batch tests; singleton assignment already happened.
        }
    }

    private static void ForcePrivateMethod(object target, string methodName)
    {
        target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(target, null);
    }

    private static void Assert(bool condition, string name)
    {
        if (condition)
        {
            _passed++;
            Log($"PASS: {name}");
            return;
        }

        _failed++;
        Failures.Add(name);
        Log($"FAIL: {name}");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        bool equal = EqualityComparer<T>.Default.Equals(expected, actual);
        Assert(equal, equal ? name : $"{name} (expected {expected}, got {actual})");
    }

    private static void Log(string message)
    {
        Debug.Log($"[CCASProgressionIntegrationTests] {message}");
    }
}
#endif
