using WalletApi.Models;
using WalletApi.Processors;

namespace WalletApi.Services;

/// <summary>
/// Service layer for wallet simulation. Orchestrates simulation and risk evaluation.
/// </summary>
public class WalletService
{
    public WalletSimulationResponse RunWalletSimulation(WalletSimulationRequest request)
    {
        var result = WalletSimulationProcessor.SimulateWalletWeek(request);
        var alerts = WalletSimulationProcessor.EvaluateWalletRisks(result);

        var transactions = request.IncludeTransactions
            ? result.Transactions.Select(t => new TransactionSimulation
            {
                Day = t.Day,
                TransactionType = t.TransactionType,
                Currency = t.Currency,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter
            }).ToList()
            : new List<TransactionSimulation>();

        return new WalletSimulationResponse
        {
            PlayerId = request.PlayerId,
            SimulationWeek = 1,
            InitialWallet = request.CurrentWallet,
            FinalWallet = result.FinalWallet,
            NetChanges = result.NetChanges,
            Transactions = transactions,
            SimulationSummary = result.Summary,
            Alerts = alerts
        };
    }
}
