using WalletApi.Models;

namespace WalletApi.Repos;

public interface IWalletRepository
{
    bool TryGet(string playerId, out WalletState wallet);
    WalletState GetOrCreate(string playerId);
    void Upsert(string playerId, WalletState wallet);
}