using System.Text.Json.Serialization;

namespace TransactionApi.Models;

public class TransactionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "earn" or "spend"

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

public class TransactionHistoryResponse
{
    [JsonPropertyName("transactions")]
    public List<TransactionResponse> Transactions { get; set; } = new();
}
