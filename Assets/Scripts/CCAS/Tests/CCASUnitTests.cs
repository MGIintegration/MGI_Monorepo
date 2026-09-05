#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CCAS.Backend;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lightweight, dependency-free test harness for CCAS' authoritative service.
/// Run from Unity with:
/// Unity -batchmode -projectPath <path> -executeMethod CCASUnitTests.RunAll -quit -logFile <path>
///
/// The tests use a dedicated player profile and clean it before and after each
/// case, so they exercise the real file-backed service without touching a
/// developer's normal local_player state.
/// </summary>
public static class CCASUnitTests
{
    private const string TestPlayerId = "__unit_test_ccas__";
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = new List<string>();

    public static void RunAll()
    {
        _passed = 0;
        _failed = 0;
        Failures.Clear();

        Log("===== CCASUnitTests: starting =====");

        Test_DevelopmentTools_BootstrapAndReset();
        var ccas = GetOrCreateCCASService();
        var dropConfig = GetOrCreateDropConfigManager();
        var catalog = GetOrCreateCardCatalogLoader();
        var progression = GetOrCreateProgressionService();

        Assert(dropConfig.config != null, "Setup: CCAS drop config loaded");
        Assert(catalog.catalog?.cards?.Length > 0, "Setup: CCAS card catalog loaded");

        Test_UnknownPack_DoesNotMutateState(ccas);
        Test_InsufficientFunds_DoesNotMutateCCASState(ccas, progression);
        Test_SuccessfulPack_ChargesPersistsAndPublishes(ccas);
        Test_DuplicateCards_AwardConfiguredXp(ccas, catalog, progression);
        Test_CatalogFailure_RefundsThePlayer(ccas, catalog);
        Test_ResetAndSeed_AffectOnlyCCASState(ccas);
        Test_ResetClearsPersistedTelemetryHistory(ccas);

        ResetAllState(ccas, progression);

        Log($"===== CCASUnitTests: {_passed} passed, {_failed} failed =====");
        if (_failed > 0)
            Log("FAILURES:\n - " + string.Join("\n - ", Failures));

        if (Application.isBatchMode)
            EditorApplication.Exit(_failed > 0 ? 1 : 0);
    }

    private static void Test_UnknownPack_DoesNotMutateState(CCASService ccas)
    {
        ResetAllState(ccas, GetOrCreateProgressionService());
        Fund(5000);
        var economy = new EconomyService();
        int coinsBefore = economy.GetWallet(TestPlayerId).coins;

        var result = ccas.OpenPack(TestPlayerId, "does_not_exist");

        Assert(!result.success && result.failureReason == "pack_not_found", "UnknownPack: returns pack_not_found");
        AssertEqual(coinsBefore, economy.GetWallet(TestPlayerId).coins, "UnknownPack: coins unchanged");
        AssertEqual(0, ccas.GetCollection(TestPlayerId).Count(), "UnknownPack: collection unchanged");
        AssertEqual(0, ccas.GetPackDropHistory(TestPlayerId).Count(), "UnknownPack: history unchanged");
    }

    private static void Test_InsufficientFunds_DoesNotMutateCCASState(CCASService ccas, ProgressionService progression)
    {
        ResetAllState(ccas, progression);
        var result = ccas.OpenPack(TestPlayerId, "bronze_pack");

        Assert(!result.success && result.failureReason == "insufficient_funds", "InsufficientFunds: returns insufficient_funds");
        AssertEqual(0, ccas.GetCollection(TestPlayerId).Count(), "InsufficientFunds: collection unchanged");
        AssertEqual(0, ccas.GetPackDropHistory(TestPlayerId).Count(), "InsufficientFunds: history unchanged");
        Assert(GetStateXp(progression) == 0, "InsufficientFunds: XP unchanged");
    }

    private static void Test_SuccessfulPack_ChargesPersistsAndPublishes(CCASService ccas)
    {
        ResetAllState(ccas, GetOrCreateProgressionService());
        Fund(5000);
        var economy = new EconomyService();
        int coinsBefore = economy.GetWallet(TestPlayerId).coins;
        int eventCount = 0;
        using (EventBus.Subscribe("buy_pack", evt =>
        {
            if (evt.player_id == TestPlayerId) eventCount++;
        }))
        {
            var result = ccas.OpenPack(TestPlayerId, "bronze_pack");
            Assert(result.success, "SuccessfulPack: succeeds");
            AssertEqual(3, result.cards.Count, "SuccessfulPack: returns configured card count");
            AssertEqual(coinsBefore - 1000, economy.GetWallet(TestPlayerId).coins, "SuccessfulPack: Economy charges configured cost");
            AssertEqual(3, ccas.GetPackDropHistory(TestPlayerId).Single().cards_pulled.Count, "SuccessfulPack: pack history persists cards");
            Assert(ccas.GetCollection(TestPlayerId).Sum(e => e.quantity) == 3, "SuccessfulPack: collection persists all cards");
            AssertEqual(1, eventCount, "SuccessfulPack: publishes one buy_pack event");
        }
    }

    private static void Test_DuplicateCards_AwardConfiguredXp(CCASService ccas, CardCatalogLoader catalog, ProgressionService progression)
    {
        ResetAllState(ccas, progression);
        Fund(5000);
        var allCards = catalog.catalog.cards
            .Where(card => card != null && !string.IsNullOrWhiteSpace(card.uid))
            .Select(card => new CardCollectionEntry { card_id = card.uid, quantity = 1 })
            .ToList();
        Assert(ccas.SeedCollectionForTesting(TestPlayerId, allCards), "DuplicateXp: seed full collection");

        int xpBefore = GetStateXp(progression);
        var result = ccas.OpenPack(TestPlayerId, "bronze_pack");
        int expectedXp = result.cardDetails.Sum(detail => detail.xpAwarded);
        int actualXp = GetStateXp(progression) - xpBefore;

        Assert(result.success, "DuplicateXp: pack succeeds");
        Assert(result.cardDetails.All(detail => detail.isDuplicate), "DuplicateXp: seeded cards are all duplicates");
        Assert(result.cardDetails.All(detail => detail.xpAwarded == ExpectedDuplicateXp(detail.rarity)), "DuplicateXp: each rarity uses configured XP");
        AssertEqual(expectedXp, actualXp, "DuplicateXp: Progression receives total duplicate XP");
    }

    private static void Test_CatalogFailure_RefundsThePlayer(CCASService ccas, CardCatalogLoader catalog)
    {
        ResetAllState(ccas, GetOrCreateProgressionService());
        Fund(5000);
        var economy = new EconomyService();
        int coinsBefore = economy.GetWallet(TestPlayerId).coins;

        var originalCatalog = catalog.catalog;
        var indexField = typeof(CardCatalogLoader).GetField("_cardsByTier", BindingFlags.NonPublic | BindingFlags.Instance);
        var originalIndex = indexField?.GetValue(catalog);

        try
        {
            catalog.catalog = new CardsCatalog { cards = Array.Empty<Card>() };
            indexField?.SetValue(catalog, new Dictionary<int, List<Card>>());

            var result = ccas.OpenPack(TestPlayerId, "bronze_pack");
            Assert(!result.success && result.failureReason == "catalog_error", "CatalogFailure: returns catalog_error");
            AssertEqual(coinsBefore, economy.GetWallet(TestPlayerId).coins, "CatalogFailure: refunds charged coins");
            AssertEqual(0, ccas.GetCollection(TestPlayerId).Count(), "CatalogFailure: collection unchanged");
            AssertEqual(0, ccas.GetPackDropHistory(TestPlayerId).Count(), "CatalogFailure: history unchanged");
        }
        finally
        {
            catalog.catalog = originalCatalog;
            indexField?.SetValue(catalog, originalIndex);
        }
    }

    private static void Test_ResetAndSeed_AffectOnlyCCASState(CCASService ccas)
    {
        ResetAllState(ccas, GetOrCreateProgressionService());
        var entry = new CardCollectionEntry { card_id = "known_card", quantity = 2 };
        Assert(ccas.SeedCollectionForTesting(TestPlayerId, new[] { entry }), "ResetSeed: seed succeeds");
        AssertEqual(2, ccas.GetCollection(TestPlayerId).Single().quantity, "ResetSeed: seeded quantity persists");
        Assert(ccas.ResetPlayerState(TestPlayerId), "ResetSeed: reset succeeds");
        AssertEqual(0, ccas.GetCollection(TestPlayerId).Count(), "ResetSeed: collection clears");
        AssertEqual(0, ccas.GetPackDropHistory(TestPlayerId).Count(), "ResetSeed: history clears");
    }

    private static void Test_ResetClearsPersistedTelemetryHistory(CCASService ccas)
    {
        const string otherPlayerId = "__unit_test_other_player__";
        string telemetryDirectory = Path.Combine(Application.persistentDataPath, "Telemetry");
        string telemetryPath = Path.Combine(telemetryDirectory, "pull_history.json");
        Directory.CreateDirectory(telemetryDirectory);
        bool telemetryFileExisted = File.Exists(telemetryPath);
        string originalTelemetryFile = telemetryFileExisted ? File.ReadAllText(telemetryPath) : null;

        try
        {
            // This writes the persisted file directly, simulating a Title Screen
            // reset where TelemetryLogger has not been loaded into the scene yet.
            File.WriteAllText(telemetryPath,
                "{\"logs\":[{\"player_id\":\"__unit_test_ccas__\",\"pack_name\":\"Old CCAS pack\"}," +
                "{\"player_id\":\"__unit_test_other_player__\",\"pack_name\":\"Keep this pack\"}]}" );

            Assert(ccas.ResetPlayerState(TestPlayerId), "ResetTelemetry: reset succeeds before telemetry loads");
            string resetFile = File.ReadAllText(telemetryPath);
            Assert(!resetFile.Contains(TestPlayerId), "ResetTelemetry: persisted Drop History clears for reset player");
            Assert(resetFile.Contains(otherPlayerId), "ResetTelemetry: other player telemetry remains");
        }
        finally
        {
            if (telemetryFileExisted)
                File.WriteAllText(telemetryPath, originalTelemetryFile);
            else if (File.Exists(telemetryPath))
                File.Delete(telemetryPath);
        }
    }

    private static void ResetAllState(CCASService ccas, ProgressionService progression)
    {
        ccas.ResetPlayerState(TestPlayerId);
        progression.ClearPlayerProgression(TestPlayerId);
        new EconomyService().ResetWallet(TestPlayerId);
        new FacilitiesService().ResetFacilityState(TestPlayerId);
        CoachesService.ResetPlayerCoachState(TestPlayerId);
    }

    private static void Fund(int coins)
    {
        new EconomyService().AddCurrency(TestPlayerId, coins, 0, 0, "ccas_unit_test_seed");
    }

    private static int GetStateXp(ProgressionService progression)
    {
        return progression.GetState(TestPlayerId, createIfMissing: true).current_xp;
    }

    private static int ExpectedDuplicateXp(string rarity)
    {
        return (rarity ?? string.Empty).ToLowerInvariant() switch
        {
            "uncommon" => 10,
            "rare" => 25,
            "epic" => 50,
            "legendary" => 100,
            _ => 5
        };
    }

    private static void Test_DevelopmentTools_BootstrapAndReset()
    {
        bool hadServiceBefore = CCASService.Instance != null;
        var service = CCASService.GetOrCreateForDevelopmentTools();

        Assert(service != null, "DevelopmentTools: returns a CCAS service");
        Assert(CCASService.Instance == service, "DevelopmentTools: registers the returned service as Instance");
        if (!hadServiceBefore)
            AssertEqual("CCASService_DevelopmentTools", service.gameObject.name, "DevelopmentTools: creates service before CCAS scene loads");
        Assert(service.ResetPlayerState(TestPlayerId), "DevelopmentTools: bootstrapped service can reset CCAS state");
    }

    private static CCASService GetOrCreateCCASService()
    {
        if (CCASService.Instance != null) return CCASService.Instance;
        var service = new GameObject("CCASService_TestHarness").AddComponent<CCASService>();
        ForceAwake(service);
        return service;
    }

    private static DropConfigManager GetOrCreateDropConfigManager()
    {
        if (DropConfigManager.Instance != null) return DropConfigManager.Instance;
        var manager = new GameObject("DropConfigManager_TestHarness").AddComponent<DropConfigManager>();
        ForceAwake(manager);
        ForcePrivateMethod(manager, "LoadConfig");
        return manager;
    }

    private static CardCatalogLoader GetOrCreateCardCatalogLoader()
    {
        if (CardCatalogLoader.Instance != null) return CardCatalogLoader.Instance;
        var loader = new GameObject("CardCatalogLoader_TestHarness").AddComponent<CardCatalogLoader>();
        ForceAwake(loader);
        ForcePrivateMethod(loader, "LoadCatalog");
        return loader;
    }

    private static ProgressionService GetOrCreateProgressionService()
    {
        if (ProgressionService.Instance != null) return ProgressionService.Instance;
        var service = new GameObject("ProgressionService_TestHarness").AddComponent<ProgressionService>();
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
            // DontDestroyOnLoad is invalid in batch-mode edit tests; singleton assignment already happened.
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
        Debug.Log($"[CCASUnitTests] {message}");
    }
}
#endif
