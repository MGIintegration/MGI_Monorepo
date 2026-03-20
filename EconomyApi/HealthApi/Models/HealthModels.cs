using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HealthApi.Models;

/// <summary>Health status levels for economy monitoring.</summary>
public static class HealthStatus
{
    public const string Healthy = "healthy";
    public const string AtRisk = "at_risk";
    public const string Critical = "critical";
}

/// <summary>Request model for economy health monitoring.</summary>
public class HealthMonitoringRequest
{
    [JsonPropertyName("player_id")]
    public Guid PlayerId { get; set; }

    [Range(1, 12)]
    [JsonPropertyName("analysis_period_weeks")]
    public int AnalysisPeriodWeeks { get; set; } = 4;

    [JsonPropertyName("include_predictions")]
    public bool IncludePredictions { get; set; } = true;

    [JsonPropertyName("include_suggestions")]
    public bool IncludeSuggestions { get; set; } = true;
}

/// <summary>Prediction of potential financial failure.</summary>
public class FailurePrediction
{
    [Range(0, int.MaxValue)]
    [JsonPropertyName("next_failure_week")]
    public int NextFailureWeek { get; set; }

    [Range(0.0, 1.0)]
    [JsonPropertyName("failure_probability")]
    public double FailureProbability { get; set; }

    [JsonPropertyName("failure_type")]
    public string FailureType { get; set; } = string.Empty;

    [JsonPropertyName("failure_reason")]
    public string FailureReason { get; set; } = string.Empty;
}

/// <summary>Suggestion for mitigating economic risks.</summary>
public class MitigationSuggestion
{
    [JsonPropertyName("suggestion_id")]
    public string SuggestionId { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("expected_impact")]
    public string ExpectedImpact { get; set; } = string.Empty;

    [JsonPropertyName("implementation_difficulty")]
    public string ImplementationDifficulty { get; set; } = string.Empty;
}

/// <summary>Economic metrics for health analysis.</summary>
public class EconomicMetrics
{
    [Range(0.0, double.MaxValue)]
    [JsonPropertyName("inflation_rate")]
    public double InflationRate { get; set; }

    [Range(0.0, 1.0)]
    [JsonPropertyName("resource_scarcity")]
    public double ResourceScarcity { get; set; }

    [JsonPropertyName("balance_trend")]
    public string BalanceTrend { get; set; } = string.Empty;

    [Range(0.0, double.MaxValue)]
    [JsonPropertyName("transaction_velocity")]
    public double TransactionVelocity { get; set; }

    [Range(0.0, 100.0)]
    [JsonPropertyName("risk_score")]
    public double RiskScore { get; set; }
}

/// <summary>Response model for economy health monitoring.</summary>
public class HealthMonitoringResponse
{
    [JsonPropertyName("player_id")]
    public Guid PlayerId { get; set; }

    [JsonPropertyName("analysis_timestamp")]
    public DateTime AnalysisTimestamp { get; set; }

    [JsonPropertyName("health_status")]
    public string HealthStatus { get; set; } = string.Empty;

    [JsonPropertyName("economic_metrics")]
    public EconomicMetrics EconomicMetrics { get; set; } = null!;

    [JsonPropertyName("failure_predictions")]
    public List<FailurePrediction> FailurePredictions { get; set; } = new();

    [JsonPropertyName("mitigation_suggestions")]
    public List<MitigationSuggestion> MitigationSuggestions { get; set; } = new();

    [JsonPropertyName("analysis_period_weeks")]
    public int AnalysisPeriodWeeks { get; set; }

    [Range(0.0, 1.0)]
    [JsonPropertyName("confidence_score")]
    public double ConfidenceScore { get; set; }

    [JsonPropertyName("next_analysis_due")]
    public DateTime NextAnalysisDue { get; set; }
}
