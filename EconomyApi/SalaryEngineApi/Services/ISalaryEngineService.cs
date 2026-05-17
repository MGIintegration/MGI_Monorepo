using SalaryEngineApi.Models;

namespace SalaryEngineApi.Services;

public interface ISalaryEngineService
{
    bool RegisterContract(SalaryContract contract);

    SalaryContract GetContractOrThrow(string playerId);

    (SalaryBreakdown breakdown, int weeklyCostCoins) CalculateWeeklyCost(string playerId, PerformanceMetrics metrics);

    Task<(bool ok, string message, int newBalance, SalaryBreakdown breakdown)> TriggerWeeklyDeduction(
        string playerId,
        PerformanceMetrics metrics
    );

    Task<BulkSalaryDeductionResponse> TriggerBulk(
        IEnumerable<(string playerId, PerformanceMetrics metrics)> items
    );
}