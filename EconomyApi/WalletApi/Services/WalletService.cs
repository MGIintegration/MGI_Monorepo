using WalletApi.Models;
using WalletApi.Repos;

namespace WalletApi.Services;

public sealed class WalletService : IWalletService
{
    private readonly IWalletRepository _repo;

    public WalletService(IWalletRepository repo)
    {
        _repo = repo;
    }

    public bool TryDisplay(string playerId, out WalletDisplayResponse response)
    {
        response = new WalletDisplayResponse();

        if (!_repo.TryGet(playerId, out var wallet))
            return false;

        response.player_id = playerId.Trim();
        response.coins = wallet.Coins;
        response.gems = wallet.Gems;
        response.credits = wallet.Credits;
        return true;
    }

    public WalletActionResponse Update(WalletUpdateRequest request)
    {
        var pid = (request.player_id ?? "").Trim();
        var txn = Guid.NewGuid().ToString();

        // Create wallet on first update 
        var wallet = _repo.GetOrCreate(pid);

        long current = GetBalance(wallet, request.currency);
        long delta = request.amount;

        if (request.operation == WalletOperation.spend)
        {
            if (current < delta)
            {
                return new WalletActionResponse
                {
                    player_id = pid,
                    transaction_id = txn,
                    currency = request.currency.ToString(),
                    new_balance = current,
                    status = "error",
                    message = $"Insufficient {request.currency}"
                };
            }

            SetBalance(wallet, request.currency, current - delta);
        }
        else // add
        {
            SetBalance(wallet, request.currency, current + delta);
        }

        _repo.Upsert(pid, wallet);

        return new WalletActionResponse
        {
            player_id = pid,
            transaction_id = txn,
            currency = request.currency.ToString(),
            new_balance = GetBalance(wallet, request.currency),
            status = "success",
            message = "Wallet updated successfully"
        };
    }

    private static long GetBalance(WalletState wallet, CurrencyType currency) =>
        currency switch
        {
            CurrencyType.coins => wallet.Coins,
            CurrencyType.gems => wallet.Gems,
            CurrencyType.credits => wallet.Credits,
            _ => wallet.Coins
        };

    private static void SetBalance(WalletState wallet, CurrencyType currency, long value)
    {
        switch (currency)
        {
            case CurrencyType.coins: wallet.Coins = value; break;
            case CurrencyType.gems: wallet.Gems = value; break;
            case CurrencyType.credits: wallet.Credits = value; break;
        }
    }
}