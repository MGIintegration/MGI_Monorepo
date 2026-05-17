namespace SalaryEngineApi.Models;

public sealed record SalaryContract(
    string PlayerId,
    decimal BaseSalary,
    decimal BonusMultiplier,
    decimal PerformanceThreshold,
    decimal MaxBonusPercentage
);