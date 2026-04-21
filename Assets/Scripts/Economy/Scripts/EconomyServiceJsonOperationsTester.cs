using System.IO;
using UnityEngine;

/// <summary>
/// Small manual test harness for EconomyService JSON operations.
/// Configure the presets in the Inspector, then run the matching context menu item.
/// </summary>
public class EconomyServiceJsonOperationsTester : MonoBehaviour
{
    private enum AddPreset
    {
        CoinsOnly,
        GemsOnly,
        CoinsAndGems,
        CoachingCreditsOnly
    }

    private enum SpendPreset
    {
        CoinsOnly,
        GemsOnly,
        CoinsAndGems,
        CoachingCreditsOnly,
        CoachHiringCoinsOnly
    }

    [Header("Target")]
    [SerializeField] private string playerId = "local_player";

    [Header("Default amounts")]
    [SerializeField] private int addCoins = 500;
    [SerializeField] private int addGems = 10;
    [SerializeField] private int addCoachingCredits = 5;
    [SerializeField] private int spendCoins = 100;
    [SerializeField] private int spendGems = 5;
    [SerializeField] private int spendCoachingCredits = 2;

    [Header("Configured actions")]
    [SerializeField] private AddPreset addPreset = AddPreset.CoinsOnly;
    [SerializeField] private SpendPreset spendPreset = SpendPreset.CoinsOnly;

    private EconomyService _service;

    private EconomyService Service => _service ??= new EconomyService();

    private void LogHeader(string title)
    {
        Debug.Log($"[EconomyJsonTest] === {title} === playerId={playerId}");
    }

    private void LogWallet(Wallet wallet)
    {
        if (wallet == null)
        {
            Debug.LogWarning("[EconomyJsonTest] No wallet yet (file missing). Use Ensure Wallet or Add configured currency.");
            return;
        }

        Debug.Log(
            $"[EconomyJsonTest] coins={wallet.coins} gems={wallet.gems} coaching_credits={wallet.coaching_credits} last_updated={wallet.last_updated}");
    }

    private void LogSpendFailure()
    {
        Debug.Log("[EconomyJsonTest] FAILED (insufficient or error)");
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

    private void AddCurrencyAndLog(string label, int coins, int gems, int coachingCredits, string source)
    {
        LogHeader(label);
        Service.AddCurrency(playerId, coins, gems, coachingCredits, source);
        LogWallet(Service.GetWallet(playerId, createIfMissing: false));
    }

    private void SpendCurrencyAndLog(string label, int coins, int gems, int coachingCredits, string source)
    {
        LogHeader(label);
        var ok = Service.TrySpend(playerId, coins, gems, coachingCredits, source, out var wallet);
        if (!ok)
        {
            LogSpendFailure();
            return;
        }

        LogWallet(wallet);
    }

    private void SpendCurrencyAndLogWithoutWallet(string label, int coins, int gems, string source)
    {
        LogHeader(label);
        var ok = Service.TrySpend(playerId, coins, gems, source);
        if (!ok)
        {
            LogSpendFailure();
            return;
        }

        LogWallet(Service.GetWallet(playerId, createIfMissing: false));
    }

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

    [ContextMenu("Write: Ensure wallet exists")]
    public void TestEnsureWallet()
    {
        LogHeader("EnsureWallet");
        LogWallet(Service.GetWallet(playerId, createIfMissing: true));
    }

    [ContextMenu("Write: Add configured currency")]
    public void TestAddConfiguredCurrency()
    {
        switch (addPreset)
        {
            case AddPreset.CoinsOnly:
                AddCurrencyAndLog($"Add coins +{addCoins}", addCoins, 0, 0, "json_test_add_coins");
                break;
            case AddPreset.GemsOnly:
                AddCurrencyAndLog($"Add gems +{addGems}", 0, addGems, 0, "json_test_add_gems");
                break;
            case AddPreset.CoinsAndGems:
                AddCurrencyAndLog(
                    $"Add coins +{addCoins}, gems +{addGems}",
                    addCoins,
                    addGems,
                    0,
                    "json_test_add_both");
                break;
            case AddPreset.CoachingCreditsOnly:
                AddCurrencyAndLog(
                    $"Add coaching credits +{addCoachingCredits}",
                    0,
                    0,
                    addCoachingCredits,
                    "json_test_add_coaching_credits");
                break;
        }
    }

    [ContextMenu("Write: Spend configured currency")]
    public void TestSpendConfiguredCurrency()
    {
        switch (spendPreset)
        {
            case SpendPreset.CoinsOnly:
                SpendCurrencyAndLog($"TrySpend coins {spendCoins}", spendCoins, 0, 0, "json_test_spend_coins");
                break;
            case SpendPreset.GemsOnly:
                SpendCurrencyAndLog($"TrySpend gems {spendGems}", 0, spendGems, 0, "json_test_spend_gems");
                break;
            case SpendPreset.CoinsAndGems:
                SpendCurrencyAndLog(
                    $"TrySpend coins {spendCoins}, gems {spendGems}",
                    spendCoins,
                    spendGems,
                    0,
                    "json_test_spend_both");
                break;
            case SpendPreset.CoachingCreditsOnly:
                SpendCurrencyAndLog(
                    $"TrySpend coaching credits {spendCoachingCredits}",
                    0,
                    0,
                    spendCoachingCredits,
                    "json_test_spend_coaching_credits");
                break;
            case SpendPreset.CoachHiringCoinsOnly:
                SpendCurrencyAndLogWithoutWallet(
                    $"TrySpend coach_hiring coins {spendCoins}",
                    spendCoins,
                    0,
                    "coach_hiring");
                break;
        }
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

    public void Ui_RunConfiguredAdd()
    {
        TestAddConfiguredCurrency();
    }

    public void Ui_RunConfiguredSpend()
    {
        TestSpendConfiguredCurrency();
    }
}
