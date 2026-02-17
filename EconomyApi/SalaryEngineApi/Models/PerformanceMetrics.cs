namespace SalaryEngineApi.Models;

public sealed record PerformanceMetrics(
    int LeadsGenerated,
    decimal ConversionRate,
    decimal QualityScore,
    decimal TeamPerformance
);