using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Temporary manual smoke test harness for EconomyService.
/// Attach to a GameObject and run via context menu or auto-run.
/// </summary>
public class EconomyServiceSmokeTestRunner : MonoBehaviour
{
    [Header("Runner Settings")]
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private string playerId = "local_player";
    [SerializeField] private bool clearExistingPlayerEconomyDataBeforeRun = true;

    private int _walletUpdatedEventCount;

    private void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(RunSmokeTest());
        }
    }

    [ContextMenu("Run EconomyService Smoke Test")]
    public void RunFromInspector()
    {
        StartCoroutine(RunSmokeTest());
    }

    private IEnumerator RunSmokeTest()
    {
        Debug.Log("[EconomySmokeTest] Starting smoke test...");
        Debug.Log($"[EconomySmokeTest] persistentDataPath: {Application.persistentDataPath}");
        Debug.Log($"[EconomySmokeTest] dataPath: {Application.dataPath}");

        _walletUpdatedEventCount = 0;

        if (clearExistingPlayerEconomyDataBeforeRun)
        {
            DeleteEconomyFilesForPlayer(playerId);
        }

        var service = new EconomyService();
        service.PublishWalletUpdatedEvents = true;

        using var subscription = EventBus.Subscribe("wallet_updated", OnWalletUpdatedEvent);

        // 1) GetWallet("local_player")
        var wallet = service.GetWallet(playerId);
        AssertCondition(wallet != null, "GetWallet returned non-null.");
        AssertCondition(wallet != null && wallet.player_id == playerId, "GetWallet player_id is correct.");

        var walletPath = GetEconomyPath(playerId, "wallet.json");
        var transactionsPath = GetEconomyPath(playerId, "wallet_transactions.json");

        AssertCondition(File.Exists(walletPath), "wallet.json was created.");
        AssertCondition(File.Exists(transactionsPath), "wallet_transactions.json was created.");

        // 2) AddCurrency("local_player", 1000, 50, "test_add")
        service.AddCurrency(playerId, 1000, 50, "test_add");
        var walletAfterAdd = service.GetWallet(playerId);

        AssertCondition(walletAfterAdd != null && walletAfterAdd.coins == 1000, "Coins after AddCurrency == 1000.");
        AssertCondition(walletAfterAdd != null && walletAfterAdd.gems == 50, "Gems after AddCurrency == 50.");
        AssertCondition(walletAfterAdd != null && walletAfterAdd.coaching_credits == 0, "Coaching credits unchanged after coin/gem AddCurrency.");

        // 3) AddCurrency("local_player", 0, 0, 15, "test_add_credits")
        service.AddCurrency(playerId, 0, 0, 15, "test_add_credits");
        var walletAfterCreditAdd = service.GetWallet(playerId);

        AssertCondition(walletAfterCreditAdd != null && walletAfterCreditAdd.coaching_credits == 15, "Coaching credits after AddCurrency == 15.");

        // 4) TrySpend("local_player", 200, 10, 5, "test_spend", out var wallet)
        var spendOk = service.TrySpend(playerId, 200, 10, 5, "test_spend", out var walletAfterSpend);
        AssertCondition(spendOk, "TrySpend succeeded for affordable amount.");
        AssertCondition(walletAfterSpend != null && walletAfterSpend.coins == 800, "Coins after TrySpend == 800.");
        AssertCondition(walletAfterSpend != null && walletAfterSpend.gems == 40, "Gems after TrySpend == 40.");
        AssertCondition(walletAfterSpend != null && walletAfterSpend.coaching_credits == 10, "Coaching credits after TrySpend == 10.");

        // 5) TrySpend should fail when insufficient.
        var beforeInsufficient = service.GetWallet(playerId);
        var failSpendOk = service.TrySpend(playerId, 999999, 999999, 999999, "test_spend_fail", out var walletAfterFail);
        var afterInsufficient = service.GetWallet(playerId);

        AssertCondition(!failSpendOk, "TrySpend returns false for insufficient balance.");
        AssertCondition(beforeInsufficient != null && afterInsufficient != null &&
                        beforeInsufficient.coins == afterInsufficient.coins &&
                        beforeInsufficient.gems == afterInsufficient.gems &&
                        beforeInsufficient.coaching_credits == afterInsufficient.coaching_credits,
            "Insufficient TrySpend does not mutate wallet.");
        AssertCondition(walletAfterFail != null, "Out wallet from failed TrySpend is still provided.");

        // 6) GetRecentTransactions("local_player", 10)
        var recent = service.GetRecentTransactions(playerId, 10).ToList();
        AssertCondition(recent.Count >= 6, "Recent transactions returned expected entries (>= 6).");

        // Validate transaction content
        ValidateTransactions(recent);

        // Validate event count (AddCurrency success + TrySpend success)
        AssertCondition(_walletUpdatedEventCount >= 3, "wallet_updated event fired for coin/gem add, coaching credit add, and successful TrySpend.");

        // Additional guard: ensure failed TrySpend did not create bad transaction.
        var hasFailedSpendSource = recent.Any(t => t != null && t.source == "test_spend_fail");
        AssertCondition(!hasFailedSpendSource, "Failed TrySpend did not create transaction entries.");

        Debug.Log("[EconomySmokeTest] Smoke test completed.");
        yield return null;
    }

    private void ValidateTransactions(List<WalletTransaction> transactions)
    {
        AssertCondition(transactions.All(t => t != null), "All transaction entries are non-null.");
        AssertCondition(transactions.All(t => !string.IsNullOrWhiteSpace(t.id) && Guid.TryParse(t.id, out _)),
            "All transactions have non-empty GUID id.");

        var earnTx = transactions.Where(t => t.type == "earn").ToList();
        var spendTx = transactions.Where(t => t.type == "spend").ToList();

        AssertCondition(earnTx.Count >= 3, "Earn transactions exist for AddCurrency.");
        AssertCondition(spendTx.Count >= 3, "Spend transactions exist for successful TrySpend.");

        AssertCondition(earnTx.Any(t => t.currency == "coins" && t.amount == 1000 && t.source == "test_add"),
            "Earn coins transaction matches expected amount/source.");
        AssertCondition(earnTx.Any(t => t.currency == "gems" && t.amount == 50 && t.source == "test_add"),
            "Earn gems transaction matches expected amount/source.");
        AssertCondition(earnTx.Any(t => t.currency == "coaching_credits" && t.amount == 15 && t.source == "test_add_credits"),
            "Earn coaching credits transaction matches expected amount/source.");
        AssertCondition(spendTx.Any(t => t.currency == "coins" && t.amount == 200 && t.source == "test_spend"),
            "Spend coins transaction matches expected amount/source.");
        AssertCondition(spendTx.Any(t => t.currency == "gems" && t.amount == 10 && t.source == "test_spend"),
            "Spend gems transaction matches expected amount/source.");
        AssertCondition(spendTx.Any(t => t.currency == "coaching_credits" && t.amount == 5 && t.source == "test_spend"),
            "Spend coaching credits transaction matches expected amount/source.");
    }

    private void OnWalletUpdatedEvent(EventBus.EventEnvelope evt)
    {
        _walletUpdatedEventCount++;
        Debug.Log($"[EconomySmokeTest] wallet_updated event #{_walletUpdatedEventCount}: {evt.payloadJson}");
    }

    private static void DeleteEconomyFilesForPlayer(string pid)
    {
        DeleteIfExists(GetEconomyPath(pid, "wallet.json"));
        DeleteIfExists(GetEconomyPath(pid, "wallet_transactions.json"));
        DeleteIfExists(GetLegacyUppercaseEconomyPath(pid, "wallet.json"));
        DeleteIfExists(GetLegacyUppercaseEconomyPath(pid, "wallet_transactions.json"));
        DeleteIfExists(GetLegacyStreamingEconomyPath("wallet.json"));
        DeleteIfExists(GetLegacyStreamingEconomyPath("wallet_transactions.json"));
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string GetEconomyPath(string pid, string fileName)
    {
        return FilePathResolver.GetEconomyPath(pid, fileName);
    }

    private static string GetLegacyUppercaseEconomyPath(string pid, string fileName)
    {
        return Path.Combine(FilePathResolver.GetPlayerDataRoot(pid), "Economy", fileName);
    }

    private static string GetLegacyStreamingEconomyPath(string fileName)
    {
        return Path.Combine(Application.dataPath, "StreamingAssets", "Economy", fileName);
    }

    private static void AssertCondition(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log($"[EconomySmokeTest][PASS] {message}");
        }
        else
        {
            Debug.LogError($"[EconomySmokeTest][FAIL] {message}");
        }
    }
}
