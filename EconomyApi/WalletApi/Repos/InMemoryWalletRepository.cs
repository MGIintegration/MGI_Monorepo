using System.Collections.Concurrent;
using WalletApi.Models;

namespace WalletApi.Repos;

public sealed class InMemoryWalletRepository : IWalletRepository
{
    private readonly ConcurrentDictionary<string, WalletState> _store = new();

    public bool TryGet(string playerId, out WalletState wallet)
        => _store.TryGetValue(Normalize(playerId), out wallet!);

    public WalletState GetOrCreate(string playerId)
    {
        var key = Normalize(playerId);
        return _store.GetOrAdd(key, _ => new WalletState());
    }

    public void Upsert(string playerId, WalletState wallet)
    {
        var key = Normalize(playerId);
        _store[key] = wallet;
    }

    private static string Normalize(string playerId) => (playerId ?? "").Trim();
}