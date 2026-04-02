using System.IO;
using UnityEngine;

/// <summary>
/// Manual test harness: multiple operations that update wallet.json and
/// wallet_transactions.json via EconomyService using FilePathResolver
/// economy paths. Use component gear menu (or assign buttons to these public methods).
/// </summary>
public class EconomyServiceJsonOperationsTester : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerId = "local_player";

    [Header("Default amounts for menu actions")]
    [SerializeField] private int addCoins = 500;
    [SerializeField] private int addGems = 10;
    [SerializeField] private int addCoachingCredits = 5;
    [SerializeField] private int spendCoins = 100;
    [SerializeField] private int spendGems = 5;
    [SerializeField] private int spendCoachingCredits = 2;
    [SerializeField] private int recentTxLimit = 25;

    private EconomyService _service;

    private EconomyService Service => _service ??= new EconomyService();

    private void LogHeader(string title)
    {
        Debug.Log($"[EconomyJsonTest] === {title} === playerId={playerId}");
    }

    private string GetEconomyFilePath(string fileName)
    {
        return FilePathResolver.GetEconomyPath(playerId, fileName);
    }

    private string GetLegacyUppercaseEconomyPath(string fileName)
    {
        return Path.Combine(FilePathResolver.GetPlayerDataRoot(playerId), "Economy", fileName);
    }

    private static string GetLegacyStreamingEconomyPath(string fileName)
    {
        return Path.Combine(Application.dataPath, "StreamingAssets", "Economy", fileName);
    }

    // --- Paths (no JSON write) ---

    [ContextMenu("Paths: Log persistent + Economy folder")]
    public void LogEconomyPaths()
    {
        LogHeader("Paths");
        Debug.Log($"[EconomyJsonTest] persistentDataPath: {Application.persistentDataPath}");
        Debug.Log($"[EconomyJsonTest] canonical wallet: {GetEconomyFilePath("wallet.json")}");
        Debug.Log($"[EconomyJsonTest] canonical tx: {GetEconomyFilePath("wallet_transactions.json")}");
        Debug.Log($"[EconomyJsonTest] legacy uppercase wallet: {GetLegacyUppercaseEconomyPath("wallet.json")}");
        Debug.Log($"[EconomyJsonTest] legacy uppercase tx: {GetLegacyUppercaseEconomyPath("wallet_transactions.json")}");
        Debug.Log($"[EconomyJsonTest] legacy StreamingAssets wallet: {GetLegacyStreamingEconomyPath("wallet.json")}");
        Debug.Log($"[EconomyJsonTest] legacy StreamingAssets tx: {GetLegacyStreamingEconomyPath("wallet_transactions.json")}");
    }

    // --- Read via service (loads JSON) ---

    [ContextMenu("Read: GetWallet + log")]
    public void TestReadWallet()
    {
        LogHeader("ReadWallet");
        var w = Service.GetWallet(playerId, createIfMissing: false);
        if (w == null)
        {
            Debug.LogWarning("[EconomyJsonTest] No wallet yet (file missing). Use CreateIfMissing or AddCurrency.");
            return;
        }

        Debug.Log(
            $"[EconomyJsonTest] coins={w.coins} gems={w.gems} coaching_credits={w.coaching_credits} last_updated={w.last_updated}");
    }

    [ContextMenu("Read: GetRecentTransactions + log")]
    public void TestReadRecentTransactions()
    {
        LogHeader("RecentTransactions");
        var list = Service.GetRecentTransactions(playerId, recentTxLimit);
        var i = 0;
        foreach (var t in list)
        {
            i++;
            Debug.Log(
                $"[EconomyJsonTest] #{i} id={t.id} type={t.type} currency={t.currency} amount={t.amount} source={t.source} ts={t.timestamp}");
        }

        Debug.Log($"[EconomyJsonTest] Count: {i}");
    }

    [ContextMenu("Read: Raw wallet.json from disk")]
    public void TestReadRawWalletJson()
    {
        LogHeader("RawWalletJson");
        var path = GetEconomyFilePath("wallet.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[EconomyJsonTest] Missing: {path}");
            return;
        }

        Debug.Log($"[EconomyJsonTest] {File.ReadAllText(path)}");
    }

    [ContextMenu("Read: Raw wallet_transactions.json from disk")]
    public void TestReadRawTransactionsJson()
    {
        LogHeader("RawTransactionsJson");
        var path = GetEconomyFilePath("wallet_transactions.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[EconomyJsonTest] Missing: {path}");
            return;
        }

        Debug.Log($"[EconomyJsonTest] {File.ReadAllText(path)}");
    }

    // --- Writes via service (updates JSON atomically) ---

    [ContextMenu("Write: Ensure wallet exists (GetWallet create)")]
    public void TestEnsureWallet()
    {
        LogHeader("EnsureWallet");
        var w = Service.GetWallet(playerId, createIfMissing: true);
        Debug.Log(w != null
            ? $"[EconomyJsonTest] OK coins={w.coins} gems={w.gems} coaching_credits={w.coaching_credits}"
            : "[EconomyJsonTest] Failed");
    }

    [ContextMenu("Write: AddCurrency (coins only)")]
    public void TestAddCoins()
    {
        LogHeader($"AddCoins +{addCoins}");
        Service.AddCurrency(playerId, addCoins, 0, "json_test_add_coins");
        TestReadWallet();
    }

    [ContextMenu("Write: AddCurrency (gems only)")]
    public void TestAddGems()
    {
        LogHeader($"AddGems +{addGems}");
        Service.AddCurrency(playerId, 0, addGems, "json_test_add_gems");
        TestReadWallet();
    }

    [ContextMenu("Write: AddCurrency (coins + gems)")]
    public void TestAddCoinsAndGems()
    {
        LogHeader($"AddBoth coins+{addCoins} gems+{addGems}");
        Service.AddCurrency(playerId, addCoins, addGems, "json_test_add_both");
        TestReadWallet();
    }

    [ContextMenu("Write: AddCurrency (coaching credits only)")]
    public void TestAddCoachingCredits()
    {
        LogHeader($"AddCoachingCredits +{addCoachingCredits}");
        Service.AddCurrency(playerId, 0, 0, addCoachingCredits, "json_test_add_coaching_credits");
        TestReadWallet();
    }

    [ContextMenu("Write: TrySpend (coins only)")]
    public void TestSpendCoins()
    {
        LogHeader($"TrySpend coins {spendCoins}");
        var ok = Service.TrySpend(playerId, spendCoins, 0, "json_test_spend_coins", out var w);
        Debug.Log(ok
            ? $"[EconomyJsonTest] SUCCESS coins={w.coins} gems={w.gems}"
            : "[EconomyJsonTest] FAILED (insufficient or error)");
    }

    [ContextMenu("Write: TrySpend (gems only)")]
    public void TestSpendGems()
    {
        LogHeader($"TrySpend gems {spendGems}");
        var ok = Service.TrySpend(playerId, 0, spendGems, "json_test_spend_gems", out var w);
        Debug.Log(ok
            ? $"[EconomyJsonTest] SUCCESS coins={w.coins} gems={w.gems}"
            : "[EconomyJsonTest] FAILED (insufficient or error)");
    }

    [ContextMenu("Write: TrySpend (coins + gems)")]
    public void TestSpendCoinsAndGems()
    {
        LogHeader($"TrySpend coins {spendCoins} gems {spendGems}");
        var ok = Service.TrySpend(playerId, spendCoins, spendGems, "json_test_spend_both", out var w);
        Debug.Log(ok
            ? $"[EconomyJsonTest] SUCCESS coins={w.coins} gems={w.gems}"
            : "[EconomyJsonTest] FAILED (insufficient or error)");
    }

    [ContextMenu("Write: TrySpend (coaching credits only)")]
    public void TestSpendCoachingCredits()
    {
        LogHeader($"TrySpend coaching credits {spendCoachingCredits}");
        var ok = Service.TrySpend(
            playerId,
            0,
            0,
            spendCoachingCredits,
            "json_test_spend_coaching_credits",
            out var w);
        Debug.Log(ok
            ? $"[EconomyJsonTest] SUCCESS coins={w.coins} gems={w.gems} coaching_credits={w.coaching_credits}"
            : "[EconomyJsonTest] FAILED (insufficient or error)");
    }

    [ContextMenu("Write: TrySpend FAIL (huge amount, should not change JSON)")]
    public void TestSpendInsufficient()
    {
        LogHeader("TrySpendInsufficient");
        var before = Service.GetWallet(playerId, false);
        var beforeCoins = before?.coins ?? -1;
        var beforeGems = before?.gems ?? -1;
        var beforeCoachingCredits = before?.coaching_credits ?? -1;

        var ok = Service.TrySpend(playerId, 9_999_999, 9_999_999, 9_999_999, "json_test_should_fail", out _);
        Debug.Log($"[EconomyJsonTest] TrySpend returned: {ok} (expected False)");

        var after = Service.GetWallet(playerId, false);
        if (before != null && after != null)
        {
            var unchanged =
                after.coins == beforeCoins &&
                after.gems == beforeGems &&
                after.coaching_credits == beforeCoachingCredits;
            Debug.Log(unchanged
                ? "[EconomyJsonTest] PASS balances unchanged"
                : "[EconomyJsonTest] FAIL balances changed");
        }
    }

    [ContextMenu("Write: Batch (add → spend → add)")]
    public void TestBatchSequence()
    {
        LogHeader("BatchSequence");
        Service.AddCurrency(playerId, 300, 20, "json_test_batch_1");
        Service.AddCurrency(playerId, 0, 0, addCoachingCredits, "json_test_batch_add_credits");
        Service.TrySpend(playerId, 50, 5, spendCoachingCredits, "json_test_batch_2", out _);
        Service.AddCurrency(playerId, 100, 0, "json_test_batch_3");
        TestReadWallet();
        TestReadRecentTransactions();
    }

    [ContextMenu("Danger: Delete Economy JSON files for playerId")]
    public void TestDeleteEconomyJsonFiles()
    {
        LogHeader("DELETE Economy JSON");
        DeleteIfExists(GetEconomyFilePath("wallet.json"));
        DeleteIfExists(GetEconomyFilePath("wallet_transactions.json"));
        DeleteIfExists(GetLegacyUppercaseEconomyPath("wallet.json"));
        DeleteIfExists(GetLegacyUppercaseEconomyPath("wallet_transactions.json"));
        DeleteIfExists(GetLegacyStreamingEconomyPath("wallet.json"));
        DeleteIfExists(GetLegacyStreamingEconomyPath("wallet_transactions.json"));
    }

    private static void DeleteIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        File.Delete(path);
        Debug.Log($"[EconomyJsonTest] Deleted {path}");
    }

    // --- Optional: call from UI Button ---

    public void Ui_AddCoins()
    {
        TestAddCoins();
    }

    public void Ui_SpendCoins()
    {
        TestSpendCoins();
    }

    public void Ui_AddCredits()
    {
        TestAddCoachingCredits();
    }

    public void Ui_SpendCredits()
    {
        TestSpendCoachingCredits();
    }

    public void Ui_RefreshLogAll()
    {
        LogEconomyPaths();
        TestReadWallet();
        TestReadRecentTransactions();
    }
}
