using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class EconomyForecastPanel : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text earningsText;
    [SerializeField] private TMP_Text expensesText;
    [SerializeField] private TMP_Text netDeltaText;

    [Header("Economy Context")]
    [SerializeField] private string playerId = "local_player";
    [SerializeField] private bool refreshOnWalletUpdatedEvent = true;
    [SerializeField] private bool refreshWhenFilesChange = true;
    [SerializeField] private float fileRefreshIntervalSeconds = 0.5f;

    [Header("Linked UI References")]
    [SerializeField] private WeeklyForecastUI weeklyForecastUI;

    private readonly EconomyForecastService _forecastService = new EconomyForecastService();
    private IDisposable _walletUpdatedSubscription;
    private float _nextFileRefreshTime;
    private string _observedFileState;

    private void OnEnable()
    {
        if (refreshOnWalletUpdatedEvent)
        {
            _walletUpdatedSubscription = EventBus.Subscribe("wallet_updated", OnWalletUpdatedEventMessage);
        }

        RefreshFromForecast();
        _observedFileState = CaptureObservedFileState();
        _nextFileRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, fileRefreshIntervalSeconds);
    }

    private void OnDisable()
    {
        _walletUpdatedSubscription?.Dispose();
        _walletUpdatedSubscription = null;
    }

    private void Update()
    {
        if (!refreshWhenFilesChange || !isActiveAndEnabled)
        {
            return;
        }

        if (Time.unscaledTime < _nextFileRefreshTime)
        {
            return;
        }

        _nextFileRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, fileRefreshIntervalSeconds);

        var currentState = CaptureObservedFileState();
        if (string.Equals(currentState, _observedFileState, StringComparison.Ordinal))
        {
            return;
        }

        _observedFileState = currentState;
        RefreshFromForecast();
    }

    public void RefreshFromForecast()
    {
        if (!_forecastService.TryGetSnapshot(playerId, out var snapshot))
        {
            snapshot = EconomyForecastService.CreateZeroSnapshot();
        }

        ApplySnapshot(snapshot);
        _observedFileState = CaptureObservedFileState();
    }

    private void ApplySnapshot(EconomyForecastSnapshot snapshot)
    {
        if (snapshot == null)
        {
            snapshot = EconomyForecastService.CreateZeroSnapshot();
        }

        if (earningsText) earningsText.text = $"Earnings: {snapshot.earnings:N0}";
        if (expensesText) expensesText.text = $"Expenses: {snapshot.totalExpenses:N0}";
        if (netDeltaText) netDeltaText.text = $"Net Delta: {snapshot.netDelta:+#;-#;0}";

        if (weeklyForecastUI != null)
        {
            weeklyForecastUI.SetForecast(snapshot.netDelta);
        }
    }

    private void OnWalletUpdatedEventMessage(EventBus.EventEnvelope evt)
    {
        if (evt == null || evt.player_id != playerId)
        {
            return;
        }

        RefreshFromForecast();
        _observedFileState = CaptureObservedFileState();
    }

    private string CaptureObservedFileState()
    {
        var effectivePlayerId = string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
        var streamingEconomyFolder = Path.Combine(Application.dataPath, "StreamingAssets", "Economy");
        var canonicalEconomyFolder = Path.GetDirectoryName(FilePathResolver.GetEconomyPath(effectivePlayerId, "wallet.json"));

        return string.Join("|",
            GetFileState(Path.Combine(streamingEconomyFolder, "economy_forecast.json")),
            GetFileState(Path.Combine(streamingEconomyFolder, "wallet.json")),
            GetFileState(Path.Combine(streamingEconomyFolder, "wallet_transactions.json")),
            GetFileState(Path.Combine(canonicalEconomyFolder ?? string.Empty, "economy_forecast.json")),
            GetFileState(Path.Combine(canonicalEconomyFolder ?? string.Empty, "wallet.json")),
            GetFileState(Path.Combine(canonicalEconomyFolder ?? string.Empty, "wallet_transactions.json")));
    }

    private static string GetFileState(string path)
    {
        if (!File.Exists(path))
        {
            return $"{path}:missing";
        }

        var info = new FileInfo(path);
        return $"{path}:{info.Length}:{info.LastWriteTimeUtc.Ticks}";
    }
}

[Serializable]
internal sealed class EconomyForecastSnapshot
{
    public int week;
    public int currentBalance;
    public int income;
    public int salary;
    public int bonus;
    public int operatingExpense;
    public int earnings;
    public int totalExpenses;
    public int netDelta;
    public int projectedBalance;
}

internal sealed class EconomyForecastService
{
    private const string DefaultPlayerId = "local_player";
    private const int LiveTransactionLimit = int.MaxValue;

    [Serializable]
    private sealed class EconomyForecastFile
    {
        public string player_id;
        public int current_balance;
        public int weeks;
        public int salary;
        public int income;
        public Dictionary<string, int> bonuses;
        public Dictionary<string, int> expenses;
        public List<ForecastWeek> forecast;
        public List<string> alerts;
        public int? base_salary;
        public int? base_income;
        public Dictionary<string, int> base_bonuses;
        public Dictionary<string, int> base_expenses;
    }

    [Serializable]
    private sealed class ForecastWeek
    {
        public int week;
        public int net_change;
        public int balance;
    }

    private const string EconomyFolderName = "Economy";
    private const string ForecastFileName = "economy_forecast.json";
    private const string StreamingAssetsFolderName = "StreamingAssets";
    private readonly EconomyService _economyService = new EconomyService();

    public static EconomyForecastSnapshot CreateZeroSnapshot()
    {
        return new EconomyForecastSnapshot
        {
            week = 0,
            currentBalance = 0,
            income = 0,
            salary = 0,
            bonus = 0,
            operatingExpense = 0,
            earnings = 0,
            totalExpenses = 0,
            netDelta = 0,
            projectedBalance = 0
        };
    }

    public bool TryGetSnapshot(out EconomyForecastSnapshot snapshot)
    {
        return TryGetSnapshot(DefaultPlayerId, out snapshot);
    }

    public bool TryGetSnapshot(string playerId, out EconomyForecastSnapshot snapshot)
    {
        snapshot = null;

        if (!TrySynchronizeForecastFile(playerId, out var forecastFile))
        {
            return false;
        }

        snapshot = BuildSnapshot(forecastFile);
        return snapshot != null;
    }

    public bool TrySynchronizeForecastFile(string playerId)
    {
        return TrySynchronizeForecastFile(playerId, out _);
    }

    private bool TrySynchronizeForecastFile(string playerId, out EconomyForecastFile forecastFile)
    {
        forecastFile = null;

        try
        {
            var effectivePlayerId = string.IsNullOrWhiteSpace(playerId) ? DefaultPlayerId : playerId;
            forecastFile = LoadForecastTemplate(effectivePlayerId) ?? CreateDefaultForecastFile(effectivePlayerId);
            NormalizeForecastFile(effectivePlayerId, forecastFile);
            SeedBaseValuesIfMissing(forecastFile);

            var liveWallet = _economyService.GetWallet(effectivePlayerId, createIfMissing: false);
            var recentCoinTransactions = _economyService
                .GetRecentTransactions(effectivePlayerId, LiveTransactionLimit)
                .Where(transaction =>
                    transaction != null &&
                    string.Equals(transaction.currency, "coins", StringComparison.Ordinal))
                .ToList();

            var earnedCoins = recentCoinTransactions
                .Where(transaction => string.Equals(transaction.type, "earn", StringComparison.Ordinal))
                .Sum(transaction => Mathf.Abs(transaction.amount));

            var spentCoins = recentCoinTransactions
                .Where(transaction => string.Equals(transaction.type, "spend", StringComparison.Ordinal))
                .Sum(transaction => Mathf.Abs(transaction.amount));

            var currentBalance = liveWallet?.coins ?? 0;
            if (IsFreshStartState(liveWallet, recentCoinTransactions))
            {
                currentBalance = 0;
                earnedCoins = 0;
                spentCoins = 0;
            }

            ApplyLiveValues(forecastFile, effectivePlayerId, currentBalance, earnedCoins, spentCoins);
            WriteForecastFilesIfChanged(effectivePlayerId, forecastFile);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EconomyForecastService] Failed to synchronize forecast JSON: {ex.Message}");
            return false;
        }
    }

    private static EconomyForecastSnapshot BuildSnapshot(EconomyForecastFile forecastFile)
    {
        if (forecastFile == null)
        {
            return CreateZeroSnapshot();
        }

        NormalizeForecastFile(
            string.IsNullOrWhiteSpace(forecastFile.player_id) ? DefaultPlayerId : forecastFile.player_id,
            forecastFile);

        var selectedWeek = forecastFile.forecast
            ?.OrderBy(entry => entry.week)
            .FirstOrDefault() ?? new ForecastWeek { week = 1, net_change = 0, balance = forecastFile.current_balance };

        var weekNumber = Mathf.Max(1, selectedWeek.week);
        var weekKey = weekNumber.ToString();

        var planIncome = forecastFile.base_income ?? forecastFile.income;
        var planSalary = forecastFile.base_salary ?? forecastFile.salary;
        var planBonus = ReadWeekValue(forecastFile.base_bonuses ?? forecastFile.bonuses, weekKey);
        var planOperatingExpense = ReadWeekValue(forecastFile.base_expenses ?? forecastFile.expenses, weekKey);

        var actualBonus = ReadWeekValue(forecastFile.bonuses, weekKey);
        var actualOperatingExpense = ReadWeekValue(forecastFile.expenses, weekKey);
        var earnings = forecastFile.income + actualBonus;
        var totalExpenses = forecastFile.salary + actualOperatingExpense;
        var netDelta = selectedWeek.net_change;
        if (netDelta == 0 && (earnings != 0 || totalExpenses != 0))
        {
            netDelta = earnings - totalExpenses;
        }

        var projectedBalance = selectedWeek.balance == 0
            ? forecastFile.current_balance
            : selectedWeek.balance;

        return new EconomyForecastSnapshot
        {
            week = weekNumber,
            currentBalance = forecastFile.current_balance,
            income = planIncome,
            salary = planSalary,
            bonus = planBonus,
            operatingExpense = planOperatingExpense,
            earnings = earnings,
            totalExpenses = totalExpenses,
            netDelta = netDelta,
            projectedBalance = projectedBalance
        };
    }

    private static int ReadWeekValue(Dictionary<string, int> values, string weekKey)
    {
        if (values == null || string.IsNullOrWhiteSpace(weekKey))
        {
            return 0;
        }

        return values.TryGetValue(weekKey, out var value) ? value : 0;
    }

    private static EconomyForecastFile LoadForecastTemplate(string playerId)
    {
        var canonicalPath = GetCanonicalForecastPath(playerId);
        var canonicalFile = ReadJsonFile<EconomyForecastFile>(canonicalPath);
        if (canonicalFile != null)
        {
            return canonicalFile;
        }

        return ReadJsonFile<EconomyForecastFile>(GetLegacyStreamingForecastPath());
    }

    private static EconomyForecastFile CreateDefaultForecastFile(string playerId)
    {
        return new EconomyForecastFile
        {
            player_id = playerId,
            current_balance = 0,
            weeks = 1,
            salary = 0,
            income = 0,
            bonuses = new Dictionary<string, int> { ["1"] = 0 },
            expenses = new Dictionary<string, int> { ["1"] = 0 },
            forecast = new List<ForecastWeek>
            {
                new ForecastWeek
                {
                    week = 1,
                    net_change = 0,
                    balance = 0
                }
            },
            alerts = new List<string>()
        };
    }

    private static void NormalizeForecastFile(string playerId, EconomyForecastFile forecastFile)
    {
        if (forecastFile == null)
        {
            return;
        }

        forecastFile.player_id = string.IsNullOrWhiteSpace(playerId) ? DefaultPlayerId : playerId;

        var orderedWeekNumbers = GetOrderedWeekNumbers(forecastFile);
        forecastFile.weeks = Mathf.Max(1, orderedWeekNumbers.Count);
        if (forecastFile.bonuses == null)
        {
            forecastFile.bonuses = new Dictionary<string, int>();
        }

        if (forecastFile.expenses == null)
        {
            forecastFile.expenses = new Dictionary<string, int>();
        }

        if (forecastFile.forecast == null)
        {
            forecastFile.forecast = new List<ForecastWeek>();
        }

        if (forecastFile.alerts == null)
        {
            forecastFile.alerts = new List<string>();
        }
    }

    private static void SeedBaseValuesIfMissing(EconomyForecastFile forecastFile)
    {
        if (forecastFile == null)
        {
            return;
        }

        if (!forecastFile.base_income.HasValue)
        {
            forecastFile.base_income = forecastFile.income;
        }

        if (!forecastFile.base_salary.HasValue)
        {
            forecastFile.base_salary = forecastFile.salary;
        }

        if (forecastFile.base_bonuses == null || forecastFile.base_bonuses.Count == 0)
        {
            forecastFile.base_bonuses = CloneWeekMap(forecastFile.bonuses);
        }

        if (forecastFile.base_expenses == null || forecastFile.base_expenses.Count == 0)
        {
            forecastFile.base_expenses = CloneWeekMap(forecastFile.expenses);
        }
    }

    private static void ApplyLiveValues(
        EconomyForecastFile forecastFile,
        string playerId,
        int currentBalance,
        int earnedCoins,
        int spentCoins)
    {
        if (forecastFile == null)
        {
            return;
        }

        var orderedWeekNumbers = GetOrderedWeekNumbers(forecastFile);
        var currentWeek = orderedWeekNumbers.FirstOrDefault();
        if (currentWeek <= 0)
        {
            currentWeek = 1;
            orderedWeekNumbers = new List<int> { 1 };
        }

        var netDelta = earnedCoins - spentCoins;

        forecastFile.player_id = playerId;
        forecastFile.current_balance = currentBalance;
        forecastFile.weeks = Mathf.Max(1, orderedWeekNumbers.Count);
        forecastFile.income = earnedCoins;
        forecastFile.salary = 0;
        forecastFile.bonuses = orderedWeekNumbers.ToDictionary(week => week.ToString(), week => 0);
        forecastFile.expenses = orderedWeekNumbers.ToDictionary(
            week => week.ToString(),
            week => week == currentWeek ? spentCoins : 0);
        forecastFile.forecast = orderedWeekNumbers
            .Select(week => new ForecastWeek
            {
                week = week,
                net_change = week == currentWeek ? netDelta : 0,
                balance = currentBalance
            })
            .ToList();
    }

    private static List<int> GetOrderedWeekNumbers(EconomyForecastFile forecastFile)
    {
        var fromForecast = forecastFile?.forecast?
            .Where(entry => entry != null && entry.week > 0)
            .Select(entry => entry.week)
            .Distinct()
            .OrderBy(week => week)
            .ToList();

        if (fromForecast != null && fromForecast.Count > 0)
        {
            return fromForecast;
        }

        var weeks = Mathf.Max(1, forecastFile?.weeks ?? 1);
        var generated = new List<int>(weeks);
        for (var week = 1; week <= weeks; week++)
        {
            generated.Add(week);
        }

        return generated;
    }

    private static Dictionary<string, int> CloneWeekMap(Dictionary<string, int> values)
    {
        if (values == null || values.Count == 0)
        {
            return new Dictionary<string, int>();
        }

        return values.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    private static void WriteForecastFilesIfChanged(string playerId, EconomyForecastFile forecastFile)
    {
        var json = JsonConvert.SerializeObject(forecastFile, Formatting.Indented);
        WriteTextIfChanged(GetCanonicalForecastPath(playerId), json);

#if UNITY_EDITOR
        WriteTextIfChanged(GetLegacyStreamingForecastPath(), json);
#endif
    }

    private static void WriteTextIfChanged(string destinationPath, string content)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        var existingContent = File.Exists(destinationPath)
            ? File.ReadAllText(destinationPath)
            : null;

        if (string.Equals(existingContent, content, StringComparison.Ordinal))
        {
            return;
        }

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = destinationPath + ".tmp";
        File.WriteAllText(tempPath, content);

        if (File.Exists(destinationPath))
        {
            var backupPath = destinationPath + ".bak";
            File.Replace(tempPath, destinationPath, backupPath, true);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            return;
        }

        File.Move(tempPath, destinationPath);
    }

    private static string GetCanonicalForecastPath(string playerId)
    {
        return FilePathResolver.GetEconomyPath(
            string.IsNullOrWhiteSpace(playerId) ? DefaultPlayerId : playerId,
            ForecastFileName);
    }

    private static string GetLegacyStreamingForecastPath()
    {
        var dir = Path.Combine(Application.dataPath, StreamingAssetsFolderName, EconomyFolderName);
        return Path.Combine(dir, ForecastFileName);
    }

    private static T ReadJsonFile<T>(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonConvert.DeserializeObject<T>(json);
    }

    private static bool IsFreshStartState(Wallet wallet, List<WalletTransaction> transactions)
    {
        var hasTransactions = transactions != null && transactions.Count > 0;
        if (hasTransactions)
        {
            return false;
        }

        if (wallet == null)
        {
            return true;
        }

        return wallet.coins == 0 &&
               wallet.gems == 0 &&
               wallet.coaching_credits == 0;
    }
}
