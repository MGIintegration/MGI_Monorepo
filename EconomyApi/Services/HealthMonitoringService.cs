using System.Text.Json;
using EconomyApi.Models;
using EconomyApi.Processors;

namespace EconomyApi.Services;

/// <summary>
/// Service layer for economy health monitoring. Runs analysis and persists logs to JSON.
/// </summary>
public class HealthMonitoringService
{
    private readonly string _healthLogsFile;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public HealthMonitoringService(string? healthLogsFile = null)
    {
        _healthLogsFile = healthLogsFile ?? "economy_health_logs.json";
        EnsureLogFileExists();
    }

    private void EnsureLogFileExists()
    {
        if (!File.Exists(_healthLogsFile))
            File.WriteAllText(_healthLogsFile, "[]");
    }

    public HealthMonitoringResponse AnalyzePlayerHealth(HealthMonitoringRequest request)
    {
        var response = HealthMonitoringProcessor.AnalyzePlayerEconomyHealth(request);
        SaveHealthLog(response);
        return response;
    }

    private void SaveHealthLog(HealthMonitoringResponse response)
    {
        try
        {
            var logs = new List<Dictionary<string, object?>>();
            if (File.Exists(_healthLogsFile))
            {
                var json = File.ReadAllText(_healthLogsFile);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    foreach (var el in doc.RootElement.EnumerateArray())
                        logs.Add(JsonSerializer.Deserialize<Dictionary<string, object?>>(el.GetRawText()) ?? new Dictionary<string, object?>());
            }

            var entry = new Dictionary<string, object?>
            {
                ["player_id"] = response.PlayerId.ToString(),
                ["analysis_timestamp"] = response.AnalysisTimestamp.ToString("O"),
                ["next_analysis_due"] = response.NextAnalysisDue.ToString("O"),
                ["health_status"] = response.HealthStatus,
                ["economic_metrics"] = response.EconomicMetrics,
                ["failure_predictions"] = response.FailurePredictions,
                ["mitigation_suggestions"] = response.MitigationSuggestions,
                ["analysis_period_weeks"] = response.AnalysisPeriodWeeks,
                ["confidence_score"] = response.ConfidenceScore
            };
            logs.Add(entry);

            File.WriteAllText(_healthLogsFile, JsonSerializer.Serialize(logs, JsonOptions));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving health log: {ex.Message}");
        }
    }

    public List<Dictionary<string, object?>> GetPlayerHealthHistory(string playerId, int limit = 10)
    {
        try
        {
            if (!File.Exists(_healthLogsFile)) return new List<Dictionary<string, object?>>();
            var json = File.ReadAllText(_healthLogsFile);
            var logs = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json)
                       ?? new List<Dictionary<string, object?>>();
            return logs
                .Where(log => log.TryGetValue("player_id", out var pid) && pid?.ToString() == playerId)
                .OrderByDescending(log => log.GetValueOrDefault("analysis_timestamp")?.ToString() ?? "")
                .Take(limit)
                .ToList();
        }
        catch
        {
            return new List<Dictionary<string, object?>>();
        }
    }

    public object GetHealthSummary(string playerId)
    {
        var history = GetPlayerHealthHistory(playerId, 1);
        if (history.Count == 0)
            return new
            {
                player_id = playerId,
                status = "no_data",
                message = "No health analysis data available",
                recommendation = "Run initial health analysis"
            };

        var latest = history[0];
        object? economicMetrics = latest.GetValueOrDefault("economic_metrics");
        var riskScore = 0.0;
        if (economicMetrics is JsonElement je && je.TryGetProperty("risk_score", out var rs))
            riskScore = rs.GetDouble();

        return new
        {
            player_id = playerId,
            current_status = latest.GetValueOrDefault("health_status") ?? "unknown",
            risk_score = riskScore,
            last_analysis = latest.GetValueOrDefault("analysis_timestamp") ?? "unknown",
            confidence = latest.GetValueOrDefault("confidence_score") ?? 0,
            active_predictions = GetArrayLength(latest, "failure_predictions"),
            suggestions_count = GetArrayLength(latest, "mitigation_suggestions"),
            next_analysis_due = latest.GetValueOrDefault("next_analysis_due") ?? "unknown"
        };
    }

    private static int GetArrayLength(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v) || v is not JsonElement je || je.ValueKind != JsonValueKind.Array)
            return 0;
        return je.GetArrayLength();
    }
}
