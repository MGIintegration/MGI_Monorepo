using System.Text.Json.Serialization;

namespace SalaryEngineApi.Models;

// ---------- Requests ----------

public sealed record SalaryContractRequest(
    [property: JsonPropertyName("player_id")] string PlayerId,
    [property: JsonPropertyName("base_salary")] decimal BaseSalary,
    [property: JsonPropertyName("bonus_multiplier")] decimal BonusMultiplier = 1m,
    [property: JsonPropertyName("performance_threshold")] decimal PerformanceThreshold = 0.75m,
    [property: JsonPropertyName("max_bonus_percentage")] decimal MaxBonusPercentage = 0.5m
);

public sealed record PerformanceMetricsRequest(
    [property: JsonPropertyName("leads_generated")] int LeadsGenerated = 0,
    [property: JsonPropertyName("conversion_rate")] decimal ConversionRate = 0m,
    [property: JsonPropertyName("quality_score")] decimal QualityScore = 0m,
    [property: JsonPropertyName("team_performance")] decimal TeamPerformance = 0m
);

public sealed record BulkSalaryDeductionItem(
    [property: JsonPropertyName("player_id")] string PlayerId,
    [property: JsonPropertyName("leads_generated")] int LeadsGenerated = 0,
    [property: JsonPropertyName("conversion_rate")] decimal ConversionRate = 0m,
    [property: JsonPropertyName("quality_score")] decimal QualityScore = 0m,
    [property: JsonPropertyName("team_performance")] decimal TeamPerformance = 0m
);

// ---------- Shared / Details ----------

public sealed record ContractDetailsResponse(
    [property: JsonPropertyName("player_id")] string PlayerId,
    [property: JsonPropertyName("base_salary")] decimal BaseSalary,
    [property: JsonPropertyName("bonus_multiplier")] decimal BonusMultiplier,
    [property: JsonPropertyName("performance_threshold")] decimal PerformanceThreshold,
    [property: JsonPropertyName("max_bonus_percentage")] decimal MaxBonusPercentage
);

public sealed record SalaryBreakdown(
    [property: JsonPropertyName("base_salary")] decimal BaseSalary,
    [property: JsonPropertyName("performance_bonus")] decimal PerformanceBonus,
    [property: JsonPropertyName("total")] decimal Total,
    [property: JsonPropertyName("metrics_score")] decimal MetricsScore
);

// ---------- Responses (Option A) ----------
// 1) Register contract: NO balance
public sealed record SalaryRegisterResponse(
    [property: JsonPropertyName("player_id")] string PlayerId,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message
);

// 2) Calculate weekly: NO balance (but includes breakdown)
public sealed record SalaryWeeklyCostResponse(
    [property: JsonPropertyName("player_id")] string PlayerId,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("salary_breakdown")] SalaryBreakdown SalaryBreakdown
);

// 3) Trigger weekly: YES balance (this is the only one that needs it)
public sealed record SalaryActionResponse(
    [property: JsonPropertyName("player_id")] string PlayerId,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("new_balance")] int NewBalance,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("salary_breakdown")] SalaryBreakdown SalaryBreakdown
);

// Bulk stays the same (no balance per player)
public sealed record BulkSalaryDeductionResponse(
    [property: JsonPropertyName("results")] Dictionary<string, bool> Results,
    [property: JsonPropertyName("processed_count")] int ProcessedCount,
    [property: JsonPropertyName("success_count")] int SuccessCount
);