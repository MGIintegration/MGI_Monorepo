using TransactionApi.Models;

namespace TransactionApi.Processors;

public static class TransactionProcessor
{
    public static List<TransactionResponse> GenerateMockTransactions(string playerId)
    {
        return new List<TransactionResponse>
        {
            new TransactionResponse
            {
                Id = new Guid("6a819ebc-8d4c-461c-84d3-6e839db91ee0"),
                UserId = playerId,
                Amount = 20,
                Currency = "coins",
                Type = "spend",
                Timestamp = DateTime.Parse("2025-11-12T10:31:33.146574"),
                Source = "coach_hiring"
            },
            new TransactionResponse
            {
                Id = new Guid("bc02c413-99f1-4612-ac54-7e240d88cf97"),
                UserId = playerId,
                Amount = 5,
                Currency = "gems",
                Type = "earn",
                Timestamp = DateTime.Parse("2025-11-12T07:31:33.147415"),
                Source = "match_win"
            },
            new TransactionResponse
            {
                Id = new Guid("783e215c-2946-42f0-a7c4-02ea438cf55c"),
                UserId = playerId,
                Amount = 150,
                Currency = "coins",
                Type = "earn",
                Timestamp = DateTime.Parse("2025-11-11T12:31:33.147428"),
                Source = "match_win"
            },
            new TransactionResponse
            {
                Id = new Guid("9d60bec6-d92d-4703-a49c-ab6b4378fd1e"),
                UserId = playerId,
                Amount = 200,
                Currency = "coins",
                Type = "earn",
                Timestamp = DateTime.Parse("2025-11-09T06:31:33.147466"),
                Source = "match_win"
            },
            new TransactionResponse
            {
                Id = new Guid("121ce16a-a5d7-476b-b42d-15591597c90a"),
                UserId = playerId,
                Amount = 15,
                Currency = "gems",
                Type = "spend",
                Timestamp = DateTime.Parse("2025-11-08T12:31:33.147472"),
                Source = "pack_purchase"
            }
        };
    }
}
