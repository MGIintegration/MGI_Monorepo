using System.ComponentModel.DataAnnotations;

namespace WalletApi.Models;

public enum CurrencyType
{
    coins,
    gems,
    credits
}

public enum WalletOperation
{
    add,
    spend
}

public sealed class WalletDisplayResponse
{
    public string player_id { get; set; } = "";
    public long coins { get; set; }
    public long gems { get; set; }
    public long credits { get; set; }
}

public sealed class WalletUpdateRequest
{
    [Required]
    public string player_id { get; set; } = "";

    [Required]
    public CurrencyType currency { get; set; }

    [Required]
    public WalletOperation operation { get; set; }

    [Range(1, int.MaxValue)]
    public int amount { get; set; }
}

public sealed class WalletActionResponse
{
    public string player_id { get; set; } = "";
    public string transaction_id { get; set; } = "";
    public string currency { get; set; } = "";
    public long new_balance { get; set; }
    public string status { get; set; } = "";   // "success" | "error"
    public string message { get; set; } = "";
}