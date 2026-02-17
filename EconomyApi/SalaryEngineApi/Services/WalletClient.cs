using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SalaryEngineApi.Services;

public sealed class WalletClient : IWalletClient
{
    private readonly HttpClient _http;

    public WalletClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<(bool ok, int newBalance, string message)> SubtractCoins(string playerId, int amount)
    {
        // WalletApi expects:
        // {
        //   "player_id": "1",
        //   "currency": 0,      // 0 = coins
        //   "operation": 1,     // 1 = subtract
        //   "amount": 1500
        // }

        var req = new
        {
            player_id = playerId,
            currency = 0,
            operation = 1,
            amount = amount
        };

        var resp = await _http.PostAsJsonAsync("/wallet/update", req);

        if (!resp.IsSuccessStatusCode)
            return (false, 0, "WalletApi call failed");

        var body = await resp.Content.ReadFromJsonAsync<WalletUpdateResponse>();

        if (body == null)
            return (false, 0, "WalletApi invalid response");

        var ok = (body.status ?? "").ToLowerInvariant() == "success";
        return (ok, body.new_balance, body.message ?? "");
    }

    private sealed class WalletUpdateResponse
    {
        public string? player_id { get; set; }
        public string? transaction_id { get; set; }
        public string? currency { get; set; }
        public int new_balance { get; set; }
        public string? status { get; set; }
        public string? message { get; set; }
    }
}