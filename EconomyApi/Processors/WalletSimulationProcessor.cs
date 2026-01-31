using EconomyApi.Models;

namespace EconomyApi.Processors;

/// <summary>
/// Core logic for simulating wallet transactions over one week:
/// daily income, expenses, and weekly bonuses.
/// </summary>
public static class WalletSimulationProcessor
{
    public class SimulationResult
    {
        public WalletState FinalWallet { get; set; } = null!;
        public WalletState NetChanges { get; set; } = null!;
        public List<TransactionRecord> Transactions { get; set; } = new();
        public SimulationSummary Summary { get; set; } = null!;
    }

    public class TransactionRecord
    {
        public int Day { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int BalanceAfter { get; set; }
    }

    public static SimulationResult SimulateWalletWeek(WalletSimulationRequest request)
    {
        var wallet = new WalletState
        {
            Coins = request.CurrentWallet.Coins,
            Gems = request.CurrentWallet.Gems,
            Credits = request.CurrentWallet.Credits
        };
        var initialWallet = new WalletState
        {
            Coins = request.CurrentWallet.Coins,
            Gems = request.CurrentWallet.Gems,
            Credits = request.CurrentWallet.Credits
        };

        var transactions = new List<TransactionRecord>();
        var dailyBalances = new DailyBalances
        {
            Coins = new List<int> { wallet.Coins },
            Gems = new List<int> { wallet.Gems },
            Credits = new List<int> { wallet.Credits }
        };

        var params_ = request.SimulationParams;

        for (var day = 1; day <= 7; day++)
        {
            if (params_.DailyIncomeCoins > 0)
            {
                wallet.Coins += params_.DailyIncomeCoins;
                transactions.Add(new TransactionRecord
                {
                    Day = day,
                    TransactionType = "daily_income",
                    Currency = "coins",
                    Amount = params_.DailyIncomeCoins,
                    BalanceAfter = wallet.Coins
                });
            }

            if (params_.DailyIncomeGems > 0)
            {
                wallet.Gems += params_.DailyIncomeGems;
                transactions.Add(new TransactionRecord
                {
                    Day = day,
                    TransactionType = "daily_income",
                    Currency = "gems",
                    Amount = params_.DailyIncomeGems,
                    BalanceAfter = wallet.Gems
                });
            }

            if (params_.DailyExpensesCoins > 0)
            {
                wallet.Coins -= params_.DailyExpensesCoins;
                transactions.Add(new TransactionRecord
                {
                    Day = day,
                    TransactionType = "daily_expense",
                    Currency = "coins",
                    Amount = -params_.DailyExpensesCoins,
                    BalanceAfter = wallet.Coins
                });
            }

            if (day == 7)
            {
                if (params_.WeeklyBonusCoins > 0)
                {
                    wallet.Coins += params_.WeeklyBonusCoins;
                    transactions.Add(new TransactionRecord
                    {
                        Day = day,
                        TransactionType = "weekly_bonus",
                        Currency = "coins",
                        Amount = params_.WeeklyBonusCoins,
                        BalanceAfter = wallet.Coins
                    });
                }
                if (params_.WeeklyBonusGems > 0)
                {
                    wallet.Gems += params_.WeeklyBonusGems;
                    transactions.Add(new TransactionRecord
                    {
                        Day = day,
                        TransactionType = "weekly_bonus",
                        Currency = "gems",
                        Amount = params_.WeeklyBonusGems,
                        BalanceAfter = wallet.Gems
                    });
                }
            }

            dailyBalances.Coins.Add(wallet.Coins);
            dailyBalances.Gems.Add(wallet.Gems);
            dailyBalances.Credits.Add(wallet.Credits);
        }

        var netChanges = new WalletState
        {
            Coins = wallet.Coins - initialWallet.Coins,
            Gems = wallet.Gems - initialWallet.Gems,
            Credits = wallet.Credits - initialWallet.Credits
        };

        var summary = new SimulationSummary
        {
            TotalTransactions = transactions.Count,
            DailyBalances = dailyBalances,
            PeakBalance = new BalanceSnapshot
            {
                Coins = dailyBalances.Coins.Max(),
                Gems = dailyBalances.Gems.Max(),
                Credits = dailyBalances.Credits.Max()
            },
            LowestBalance = new BalanceSnapshot
            {
                Coins = dailyBalances.Coins.Min(),
                Gems = dailyBalances.Gems.Min(),
                Credits = dailyBalances.Credits.Min()
            }
        };

        return new SimulationResult
        {
            FinalWallet = wallet,
            NetChanges = netChanges,
            Transactions = transactions,
            Summary = summary
        };
    }

    public static List<string> EvaluateWalletRisks(SimulationResult result)
    {
        var alerts = new List<string>();
        var finalWallet = result.FinalWallet;
        var lowest = result.Summary.LowestBalance;

        if (lowest.Coins < 100)
            alerts.Add($"Week simulation: Coin balance dropped to {lowest.Coins} (below 100 threshold)");
        if (lowest.Gems < 5)
            alerts.Add($"Week simulation: Gem balance dropped to {lowest.Gems} (below 5 threshold)");
        if (lowest.Credits < 10)
            alerts.Add($"Week simulation: Credit balance dropped to {lowest.Credits} (below 10 threshold)");

        if (finalWallet.Coins < 0)
            alerts.Add($"Week simulation: Coin balance went negative: {finalWallet.Coins}");
        if (finalWallet.Gems < 0)
            alerts.Add($"Week simulation: Gem balance went negative: {finalWallet.Gems}");
        if (finalWallet.Credits < 0)
            alerts.Add($"Week simulation: Credit balance went negative: {finalWallet.Credits}");

        return alerts;
    }
}
