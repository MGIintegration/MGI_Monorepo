using SalaryEngineApi.Models;
using SalaryEngineApi.Repos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalaryEngineApi.Services;

public sealed class SalaryEngineService : ISalaryEngineService
{
    private readonly ISalaryContractRepository _contracts;
    private readonly IWalletClient _wallet;

    public SalaryEngineService(ISalaryContractRepository contracts, IWalletClient wallet)
    {
        _contracts = contracts;
        _wallet = wallet;
    }

    public bool RegisterContract(SalaryContract contract)
    {
        var pid = Normalize(contract.PlayerId);
        return _contracts.Upsert(contract with { PlayerId = pid });
    }

    public SalaryContract GetContractOrThrow(string playerId)
    {
        var pid = Normalize(playerId);
        var c = _contracts.Get(pid);
        if (c is null) throw new KeyNotFoundException("salary contract not found");
        return c;
    }

    public (SalaryBreakdown breakdown, int weeklyCostCoins) CalculateWeeklyCost(string playerId, PerformanceMetrics metrics)
    {
        var c = GetContractOrThrow(playerId);

        var score = CalculateScore(metrics);

        var weeklyBase = c.BaseSalary / 4m;

        decimal performanceBonus = 0m;
        if (score >= c.PerformanceThreshold)
        {
            var bonusPct = Math.Min(score * c.BonusMultiplier, c.MaxBonusPercentage);
            performanceBonus = weeklyBase * bonusPct;
        }

        var total = weeklyBase + performanceBonus;
        var weeklyCostCoins = (int)Math.Round(total, MidpointRounding.AwayFromZero);

        var breakdown = new SalaryBreakdown(
            BaseSalary: weeklyBase,
            PerformanceBonus: performanceBonus,
            Total: total,
            MetricsScore: score
        );

        return (breakdown, weeklyCostCoins);
    }

    public async Task<(bool ok, string message, int newBalance, SalaryBreakdown breakdown)> TriggerWeeklyDeduction(
        string playerId,
        PerformanceMetrics metrics
    )
    {
        var pid = Normalize(playerId);

        var (breakdown, weeklyCost) = CalculateWeeklyCost(pid, metrics);

        // Weekly salary is a DEDUCTION in coins
        var (ok, newBal, walletMsg) = await _wallet.SubtractCoins(pid, weeklyCost);

        if (!ok)
        {
            var msg = string.IsNullOrWhiteSpace(walletMsg)
                ? "Salary deduction failed: WalletApi call failed"
                : $"Salary deduction failed: {walletMsg}";

            return (false, msg, newBal, breakdown);
        }

        return (true, "Weekly salary deducted successfully", newBal, breakdown);
    }

    public async Task<BulkSalaryDeductionResponse> TriggerBulk(IEnumerable<(string playerId, PerformanceMetrics metrics)> items)
    {
        var results = new Dictionary<string, bool>();
        var processed = 0;
        var success = 0;

        foreach (var (pidRaw, metrics) in items)
        {
            processed++;
            var pid = Normalize(pidRaw);

            if (!_contracts.Exists(pid))
            {
                results[pid] = false;
                continue;
            }

            var (ok, _, _, _) = await TriggerWeeklyDeduction(pid, metrics);
            results[pid] = ok;
            if (ok) success++;
        }

        return new BulkSalaryDeductionResponse(results, processed, success);
    }

    private static decimal CalculateScore(PerformanceMetrics m)
    {
        // leads: min(leads/100, 1) * 0.3
        // conversion_rate * 0.3
        // quality_score/100 * 0.2
        // team_performance/100 * 0.2

        var leadScore = Math.Min((decimal)m.LeadsGenerated / 100m, 1m) * 0.3m;
        var conversionScore = Clamp01(m.ConversionRate) * 0.3m;
        var qualityScore = Clamp01(m.QualityScore / 100m) * 0.2m;
        var teamScore = Clamp01(m.TeamPerformance / 100m) * 0.2m;

        return leadScore + conversionScore + qualityScore + teamScore;
    }

    private static decimal Clamp01(decimal v) => v < 0m ? 0m : (v > 1m ? 1m : v);

    private static string Normalize(string s) => (s ?? "").Trim();
}