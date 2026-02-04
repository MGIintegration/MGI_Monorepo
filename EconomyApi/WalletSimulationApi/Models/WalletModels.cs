using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WalletSimulationApi.Models;

/// <summary>Current wallet state for simulation.</summary>
public class WalletState
{
    [Range(0, int.MaxValue)]
    [JsonPropertyName("coins")]
    public int Coins { get; set; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("gems")]
    public int Gems { get; set; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("credits")]
    public int Credits { get; set; }
}

/// <summary>Parameters for wallet simulation.</summary>
public class SimulationParameters
{
    [Range(0, int.MaxValue)]
    [JsonPropertyName("daily_income_coins")]
    public int DailyIncomeCoins { get; set; } = 100;

    [Range(0, int.MaxValue)]
    [JsonPropertyName("daily_income_gems")]
    public int DailyIncomeGems { get; set; } = 2;

    [Range(0, int.MaxValue)]
    [JsonPropertyName("daily_expenses_coins")]
    public int DailyExpensesCoins { get; set; } = 50;

    [Range(0, int.MaxValue)]
    [JsonPropertyName("weekly_bonus_coins")]
    public int WeeklyBonusCoins { get; set; } = 200;

    [Range(0, int.MaxValue)]
    [JsonPropertyName("weekly_bonus_gems")]
    public int WeeklyBonusGems { get; set; } = 10;

    [JsonPropertyName("special_events")]
    public Dictionary<string, int> SpecialEvents { get; set; } = new();
}

/// <summary>Request model for wallet simulation.</summary>
public class WalletSimulationRequest
{
    [JsonPropertyName("player_id")]
    public Guid PlayerId { get; set; }

    [JsonPropertyName("current_wallet")]
    public WalletState CurrentWallet { get; set; } = null!;

    [JsonPropertyName("simulation_params")]
    public SimulationParameters SimulationParams { get; set; } = null!;

    [JsonPropertyName("include_transactions")]
    public bool IncludeTransactions { get; set; } = true;
}

/// <summary>Individual transaction simulation.</summary>
public class TransactionSimulation
{
    [Range(1, 7)]
    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("transaction_type")]
    public string TransactionType { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("balance_after")]
    public int BalanceAfter { get; set; }
}

/// <summary>Response model for wallet simulation.</summary>
public class WalletSimulationResponse
{
    [JsonPropertyName("player_id")]
    public Guid PlayerId { get; set; }

    [JsonPropertyName("simulation_week")]
    public int SimulationWeek { get; set; } = 1;

    [JsonPropertyName("initial_wallet")]
    public WalletState InitialWallet { get; set; } = null!;

    [JsonPropertyName("final_wallet")]
    public WalletState FinalWallet { get; set; } = null!;

    [JsonPropertyName("net_changes")]
    public WalletState NetChanges { get; set; } = null!;

    [JsonPropertyName("transactions")]
    public List<TransactionSimulation> Transactions { get; set; } = new();

    [JsonPropertyName("simulation_summary")]
    public SimulationSummary SimulationSummary { get; set; } = null!;

    [JsonPropertyName("alerts")]
    public List<string> Alerts { get; set; } = new();
}

/// <summary>Summary of simulation run.</summary>
public class SimulationSummary
{
    [JsonPropertyName("total_transactions")]
    public int TotalTransactions { get; set; }

    [JsonPropertyName("daily_balances")]
    public DailyBalances DailyBalances { get; set; } = null!;

    [JsonPropertyName("peak_balance")]
    public BalanceSnapshot PeakBalance { get; set; } = null!;

    [JsonPropertyName("lowest_balance")]
    public BalanceSnapshot LowestBalance { get; set; } = null!;
}

public class DailyBalances
{
    [JsonPropertyName("coins")]
    public List<int> Coins { get; set; } = new();

    [JsonPropertyName("gems")]
    public List<int> Gems { get; set; } = new();

    [JsonPropertyName("credits")]
    public List<int> Credits { get; set; } = new();
}

public class BalanceSnapshot
{
    [JsonPropertyName("coins")]
    public int Coins { get; set; }

    [JsonPropertyName("gems")]
    public int Gems { get; set; }

    [JsonPropertyName("credits")]
    public int Credits { get; set; }
}
