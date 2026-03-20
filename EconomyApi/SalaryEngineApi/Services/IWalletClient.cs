using System.Threading.Tasks;

namespace SalaryEngineApi.Services;

public interface IWalletClient
{
    // Calls WalletApi POST /wallet/update with:
    // currency = 0 (coins), operation = 1 (subtract)
    Task<(bool ok, int newBalance, string message)> SubtractCoins(string playerId, int amount);
}