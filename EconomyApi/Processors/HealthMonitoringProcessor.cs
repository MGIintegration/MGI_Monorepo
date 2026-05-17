using EconomyApi.Models;

namespace EconomyApi.Processors;

/// <summary>
/// Health monitoring processor: generates failure predictions
/// and mitigation suggestions based on economic metrics.
/// </summary>
public static class HealthMonitoringProcessor
{
    private static readonly Random Rng = new();

    public static HealthMonitoringResponse AnalyzePlayerEconomyHealth(HealthMonitoringRequest request)
    {
        var playerId = request.PlayerId;
        var analysisPeriodWeeks = request.AnalysisPeriodWeeks;

        var inflationRate = Math.Round(Rng.NextDouble() * (0.045 - 0.015) + 0.015, 4);
        var resourceScarcity = Math.Round(Rng.NextDouble() * (0.4 - 0.1) + 0.1, 3);
        var balanceTrend = new[] { "stable", "increasing", "decreasing" }[Rng.Next(3)];
        var transactionVelocity = Math.Round(Rng.NextDouble() * (6.0 - 1.5) + 1.5, 2);

        var riskScore = CalculateRiskScore(inflationRate, resourceScarcity, balanceTrend, transactionVelocity);
        var healthStatus = DetermineHealthStatus(riskScore);

        var economicMetrics = new EconomicMetrics
        {
            InflationRate = inflationRate,
            ResourceScarcity = resourceScarcity,
            BalanceTrend = balanceTrend,
            TransactionVelocity = transactionVelocity,
            RiskScore = riskScore
        };

        var failurePredictions = request.IncludePredictions
            ? GenerateFailurePredictions(healthStatus, economicMetrics)
            : new List<FailurePrediction>();

        var mitigationSuggestions = request.IncludeSuggestions
            ? GenerateMitigationSuggestions(healthStatus, economicMetrics, failurePredictions)
            : new List<MitigationSuggestion>();

        var analysisTimestamp = DateTime.UtcNow;
        var nextAnalysisDue = analysisTimestamp.AddDays(7 * analysisPeriodWeeks);

        return new HealthMonitoringResponse
        {
            PlayerId = playerId,
            AnalysisTimestamp = analysisTimestamp,
            HealthStatus = healthStatus,
            EconomicMetrics = economicMetrics,
            FailurePredictions = failurePredictions,
            MitigationSuggestions = mitigationSuggestions,
            AnalysisPeriodWeeks = analysisPeriodWeeks,
            ConfidenceScore = Math.Round(Rng.NextDouble() * (0.95 - 0.75) + 0.75, 2),
            NextAnalysisDue = nextAnalysisDue
        };
    }

    private static double CalculateRiskScore(double inflation, double scarcity, string trend, double velocity)
    {
        var riskScore = 0.0;

        if (inflation > 0.04) riskScore += 25;
        else if (inflation > 0.03) riskScore += 20;
        else if (inflation > 0.025) riskScore += 15;
        else if (inflation > 0.02) riskScore += 10;

        if (scarcity > 0.35) riskScore += 20;
        else if (scarcity > 0.25) riskScore += 15;
        else if (scarcity > 0.15) riskScore += 10;

        riskScore += trend switch
        {
            "decreasing" => 30,
            "stable" => 5,
            _ => 0
        };

        if (velocity > 5) riskScore += 15;
        else if (velocity > 3) riskScore += 10;
        else if (velocity < 2) riskScore += 10;

        riskScore += Rng.NextDouble() * 15 - 5;
        return Math.Max(0, Math.Min(riskScore, 100));
    }

    private static string DetermineHealthStatus(double riskScore)
    {
        if (riskScore >= 70) return HealthStatus.Critical;
        if (riskScore >= 40) return HealthStatus.AtRisk;
        return HealthStatus.Healthy;
    }

    private static List<FailurePrediction> GenerateFailurePredictions(string healthStatus, EconomicMetrics metrics)
    {
        var predictions = new List<FailurePrediction>();

        if (healthStatus == HealthStatus.Critical)
        {
            predictions.Add(new FailurePrediction
            {
                NextFailureWeek = Rng.Next(1, 4),
                FailureProbability = Math.Round(Rng.NextDouble() * 0.2 + 0.7, 2),
                FailureType = "balance_depletion",
                FailureReason = "Critical risk score indicates imminent economic failure"
            });
            if (metrics.InflationRate > 0.03)
                predictions.Add(new FailurePrediction
                {
                    NextFailureWeek = Rng.Next(2, 5),
                    FailureProbability = Math.Round(Rng.NextDouble() * 0.2 + 0.6, 2),
                    FailureType = "inflation_crisis",
                    FailureReason = "High inflation rate threatens economic stability"
                });
        }
        else if (healthStatus == HealthStatus.AtRisk)
        {
            predictions.Add(new FailurePrediction
            {
                NextFailureWeek = Rng.Next(3, 7),
                FailureProbability = Math.Round(Rng.NextDouble() * 0.3 + 0.4, 2),
                FailureType = "balance_depletion",
                FailureReason = "Declining balance trend suggests future economic stress"
            });
            if (metrics.ResourceScarcity > 0.25)
                predictions.Add(new FailurePrediction
                {
                    NextFailureWeek = Rng.Next(4, 9),
                    FailureProbability = Math.Round(Rng.NextDouble() * 0.3 + 0.3, 2),
                    FailureType = "resource_scarcity",
                    FailureReason = "Resource scarcity may lead to economic constraints"
                });
        }
        else
        {
            if (Rng.NextDouble() < 0.6)
                predictions.Add(new FailurePrediction
                {
                    NextFailureWeek = Rng.Next(6, 13),
                    FailureProbability = Math.Round(Rng.NextDouble() * 0.3 + 0.1, 2),
                    FailureType = "balance_depletion",
                    FailureReason = "Long-term monitoring suggests potential future risks"
                });
            if (metrics.InflationRate > 0.025 && Rng.NextDouble() < 0.4)
                predictions.Add(new FailurePrediction
                {
                    NextFailureWeek = Rng.Next(8, 13),
                    FailureProbability = Math.Round(Rng.NextDouble() * 0.3 + 0.2, 2),
                    FailureType = "inflation_crisis",
                    FailureReason = "Moderate inflation may escalate without intervention"
                });
        }

        return predictions;
    }

    private static List<MitigationSuggestion> GenerateMitigationSuggestions(
        string healthStatus,
        EconomicMetrics metrics,
        List<FailurePrediction> predictions)
    {
        var suggestions = new List<MitigationSuggestion>();

        if (healthStatus == HealthStatus.Critical)
        {
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "emergency_intervention_001",
                Category = "balance",
                Priority = "critical",
                Description = "Implement emergency economic intervention measures immediately",
                ExpectedImpact = "Prevent economic collapse and stabilize critical metrics",
                ImplementationDifficulty = "high"
            });
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "expense_reduction_001",
                Category = "expense",
                Priority = "critical",
                Description = "Drastically reduce all non-essential expenses",
                ExpectedImpact = "Reduce economic pressure by 40-60%",
                ImplementationDifficulty = "medium"
            });
        }
        else if (healthStatus == HealthStatus.AtRisk)
        {
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "balance_stabilization_001",
                Category = "balance",
                Priority = "high",
                Description = "Implement measures to stabilize declining balance trend",
                ExpectedImpact = "Reverse negative balance trend within 2-3 weeks",
                ImplementationDifficulty = "medium"
            });
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "income_boost_001",
                Category = "income",
                Priority = "high",
                Description = "Increase income sources or boost existing income rates",
                ExpectedImpact = "Increase daily income by 25-40%",
                ImplementationDifficulty = "low"
            });
        }
        else
        {
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "preventive_monitoring_001",
                Category = "monitoring",
                Priority = "medium",
                Description = "Implement enhanced monitoring to detect early warning signs",
                ExpectedImpact = "Early detection of potential issues",
                ImplementationDifficulty = "low"
            });
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "optimization_001",
                Category = "income",
                Priority = "low",
                Description = "Optimize existing income streams for better efficiency",
                ExpectedImpact = "Improve income efficiency by 10-15%",
                ImplementationDifficulty = "low"
            });
        }

        if (metrics.InflationRate > 0.03)
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "inflation_control_001",
                Category = "expense",
                Priority = "high",
                Description = "Implement inflation control measures",
                ExpectedImpact = "Reduce inflation rate by 20-30%",
                ImplementationDifficulty = "medium"
            });

        if (metrics.ResourceScarcity > 0.25)
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "resource_management_001",
                Category = "income",
                Priority = "medium",
                Description = "Improve resource management and generation",
                ExpectedImpact = "Reduce scarcity by 25-40%",
                ImplementationDifficulty = "low"
            });

        if (metrics.TransactionVelocity < 2)
            suggestions.Add(new MitigationSuggestion
            {
                SuggestionId = "activity_boost_001",
                Category = "income",
                Priority = "medium",
                Description = "Encourage more frequent economic activity",
                ExpectedImpact = "Increase transaction velocity by 50%",
                ImplementationDifficulty = "low"
            });

        foreach (var p in predictions)
        {
            if (p.FailureType == "balance_depletion")
                suggestions.Add(new MitigationSuggestion
                {
                    SuggestionId = "balance_protection_001",
                    Category = "balance",
                    Priority = "high",
                    Description = "Implement balance protection measures",
                    ExpectedImpact = "Prevent balance depletion",
                    ImplementationDifficulty = "medium"
                });
            else if (p.FailureType == "inflation_crisis")
                suggestions.Add(new MitigationSuggestion
                {
                    SuggestionId = "inflation_prevention_001",
                    Category = "expense",
                    Priority = "high",
                    Description = "Prevent inflation crisis through expense management",
                    ExpectedImpact = "Avoid inflation crisis",
                    ImplementationDifficulty = "high"
                });
        }

        return suggestions;
    }
}
