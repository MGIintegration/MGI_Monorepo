using System.Text.Json.Serialization;

namespace ForecastApi.Models;

public class ForecastRequest
{
    /// <summary>Unique ID of the player.</summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [JsonPropertyName("player_id")]
    public Guid PlayerId { get; set; }

    /// <summary>Starting balance for the simulation.</summary>
    /// <example>1000</example>
    [JsonPropertyName("current_balance")]
    public int CurrentBalance { get; set; }

    /// <summary>Number of weeks to simulate.</summary>
    /// <example>4</example>
    [JsonPropertyName("weeks")]
    public int Weeks { get; set; }

    /// <summary>Weekly salary for the player.</summary>
    /// <example>100</example>
    [JsonPropertyName("salary")]
    public int Salary { get; set; }

    /// <summary>Reference gross income (not used in balance calculation).</summary>
    /// <example>200</example>
    [JsonPropertyName("income")]
    public int Income { get; set; }

    /// <summary>Lookup of weekly expenses by week number.</summary>
    /// <example>{"1": 30, "2": 40, "3": 50}</example>
    [JsonPropertyName("expenses")]
    public Dictionary<string, int> Expenses { get; set; } = new();

    /// <summary>Lookup of weekly bonuses by week number.</summary>
    /// <example>{"1": 50, "2": 0}</example>
    [JsonPropertyName("bonuses")]
    public Dictionary<string, int> Bonuses { get; set; } = new();
}

public class WeeklyForecast
{
    [JsonPropertyName("week")]
    public int Week { get; set; }

    [JsonPropertyName("net_change")]
    public int NetChange { get; set; }

    [JsonPropertyName("balance")]
    public int Balance { get; set; }
}

public class EconomyForecastResponse
{
    [JsonPropertyName("player_id")]
    public Guid PlayerId { get; set; }

    [JsonPropertyName("current_balance")]
    public int CurrentBalance { get; set; }

    [JsonPropertyName("weeks")]
    public int Weeks { get; set; }

    [JsonPropertyName("salary")]
    public int Salary { get; set; }

    [JsonPropertyName("income")]
    public int Income { get; set; }

    [JsonPropertyName("expenses")]
    public Dictionary<string, int> Expenses { get; set; } = new();

    [JsonPropertyName("bonuses")]
    public Dictionary<string, int> Bonuses { get; set; } = new();

    [JsonPropertyName("forecast")]
    public List<WeeklyForecast> Forecast { get; set; } = new();

    [JsonPropertyName("alerts")]
    public List<string> Alerts { get; set; } = new();
}
