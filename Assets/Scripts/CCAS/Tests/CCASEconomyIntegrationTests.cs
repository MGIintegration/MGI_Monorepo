#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CCAS.Backend;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Repeatable CCAS -> Economy workflow checks using real file-backed services.
/// Run with:
/// Unity -batchmode -projectPath <path> -executeMethod CCASEconomyIntegrationTests.RunAll -quit -logFile <path>
/// </summary>
public static class CCASEconomyIntegrationTests
{
    private const string TestPlayerId = "__integration_test_ccas_economy__";
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = new List<string>();

    public static void RunAll()
    {
        _passed = 0;
        _failed = 0;
        Failures.Clear();
        Log("===== CCASEconomyIntegrationTests: starting =====");

        var ccas = GetOrCreateCCASService();
        var dropConfig = GetOrCreateDropConfigManager();
        GetOrCreateCardCatalogLoader();

        Assert(dropConfig.config != null, "Setup: CCAS pack configuration loaded");
        Test_PackPurchase_ChargesEconomyAndPersistsCCAS(ccas, dropConfig);
        Test_InsufficientFunds_LeavesEconomyAndCCASUnchanged(ccas);

        ResetWorkflowState(ccas);
        Log($"===== CCASEconomyIntegrationTests: {_passed} passed, {_failed} failed =====");
        if (_failed > 0)
            Log("FAILURES:\n - " + string.Join("\n - ", Failures));

        if (Application.isBatchMode)
            EditorApplication.Exit(_failed > 0 ? 1 : 0);
    }

    private static void Test_PackPurchase_ChargesEconomyAndPersistsCCAS(CCASService ccas, DropConfigManager dropConfig)
    {
        ResetWorkflowState(ccas);
        const string packTypeId = "bronze_pack";
        var pack = dropConfig.config.pack_types[packTypeId];
        var economy = new EconomyService();
        economy.AddCurrency(TestPlayerId, pack.cost + 500, 0, 0, "integration_test_seed");

        int eventCount = 0;
        using (EventBus.Subscribe("buy_pack", evt =>
        {
            if (evt.player_id == TestPlayerId)
                eventCount++;
        }))
        {
            var result = ccas.OpenPack(TestPlayerId, packTypeId);
            var wallet = economy.GetWallet(TestPlayerId, createIfMissing: false);
            var transactions = economy.GetRecentTransactions(TestPlayerId, 10).ToList();
            var history = ccas.GetPackDropHistory(TestPlayerId).ToList();

            Assert(result.success, "PackPurchase: CCAS reports success");
            AssertEqual(pack.guaranteed_cards, result.cards.Count, "PackPurchase: returns configured card count");
            AssertEqual(500, wallet.coins, "PackPurchase: Economy deducts exactly the pack cost");
            Assert(transactions.Any(tx => tx.type == "spend" && tx.source == "pack_purchase" &&
                                         tx.currency == "coins" && tx.amount == pack.cost),
                "PackPurchase: Economy writes pack_purchase transaction");
            AssertEqual(pack.guaranteed_cards, ccas.GetCollection(TestPlayerId).Sum(entry => entry.quantity),
                "PackPurchase: CCAS collection contains every pulled card");
            AssertEqual(1, history.Count, "PackPurchase: CCAS appends one pack history entry");
            Assert(history.Count == 1 && history[0].cost_paid.coins == pack.cost,
                "PackPurchase: history records the exact cost paid");
            AssertEqual(1, eventCount, "PackPurchase: publishes one buy_pack event");
        }
    }

    private static void Test_InsufficientFunds_LeavesEconomyAndCCASUnchanged(CCASService ccas)
    {
        ResetWorkflowState(ccas);
        var economy = new EconomyService();
        int eventCount = 0;

        using (EventBus.Subscribe("buy_pack", evt =>
        {
            if (evt.player_id == TestPlayerId)
                eventCount++;
        }))
        {
            var result = ccas.OpenPack(TestPlayerId, "bronze_pack");
            var wallet = economy.GetWallet(TestPlayerId, createIfMissing: false);

            Assert(!result.success && result.failureReason == "insufficient_funds",
                "InsufficientFunds: CCAS rejects the purchase");
            AssertEqual(0, wallet.coins, "InsufficientFunds: Economy balance remains unchanged");
            AssertEqual(0, ccas.GetCollection(TestPlayerId).Count(), "InsufficientFunds: CCAS collection remains empty");
            AssertEqual(0, ccas.GetPackDropHistory(TestPlayerId).Count(), "InsufficientFunds: CCAS history remains empty");
            AssertEqual(0, eventCount, "InsufficientFunds: does not publish buy_pack");
        }
    }

    private static void ResetWorkflowState(CCASService ccas)
    {
        ccas.ResetPlayerState(TestPlayerId);
        new EconomyService().ResetWallet(TestPlayerId);
    }

    private static CCASService GetOrCreateCCASService()
    {
        if (CCASService.Instance != null)
            return CCASService.Instance;

        var service = new GameObject("CCASEconomyIntegration_CCASSvc").AddComponent<CCASService>();
        ForceAwake(service);
        return service;
    }

    private static DropConfigManager GetOrCreateDropConfigManager()
    {
        if (DropConfigManager.Instance != null)
            return DropConfigManager.Instance;

        var manager = new GameObject("CCASEconomyIntegration_DropConfig").AddComponent<DropConfigManager>();
        ForceAwake(manager);
        ForcePrivateMethod(manager, "LoadConfig");
        return manager;
    }

    private static CardCatalogLoader GetOrCreateCardCatalogLoader()
    {
        if (CardCatalogLoader.Instance != null)
            return CardCatalogLoader.Instance;

        var loader = new GameObject("CCASEconomyIntegration_CardCatalog").AddComponent<CardCatalogLoader>();
        ForceAwake(loader);
        ForcePrivateMethod(loader, "LoadCatalog");
        return loader;
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
        Debug.Log($"[CCASEconomyIntegrationTests] {message}");
    }
}
#endif
