using TransactionApi.Models;
using TransactionApi.Processors;

namespace TransactionApi.Services;

public class TransactionService
{
    public List<TransactionResponse> GetHistory(string playerId)
    {
        return TransactionProcessor.GenerateMockTransactions(playerId);
    }
}
