using System.Collections.Concurrent;
using SalaryEngineApi.Models;

namespace SalaryEngineApi.Repos;

public sealed class InMemorySalaryContractRepository : ISalaryContractRepository
{
    private readonly ConcurrentDictionary<string, SalaryContract> _contracts = new();

    public bool Upsert(SalaryContract contract)
    {
        _contracts[Normalize(contract.PlayerId)] = contract with { PlayerId = Normalize(contract.PlayerId) };
        return true;
    }

    public SalaryContract? Get(string playerId)
    {
        _contracts.TryGetValue(Normalize(playerId), out var c);
        return c;
    }

    public bool Exists(string playerId) => _contracts.ContainsKey(Normalize(playerId));

    private static string Normalize(string s) => (s ?? "").Trim();
}