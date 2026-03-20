using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Single entry point for all wallet operations for a player.
/// Persists wallet and transaction state under FilePathResolver economy paths.
/// </summary>
public class EconomyService
{
    [Serializable]
    private class WalletUpdatedPayload
    {
        public string player_id;
        public int coins;
        public int gems;
        public int coaching_credits;
        public string source;
        public string timestamp;
    }

    private const string WalletFileName = "wallet.json";
    private const string TransactionsFileName = "wallet_transactions.json";
    private const string EconomyFolderName = "Economy";
    private const string StreamingAssetsFolderName = "StreamingAssets";

    /// <summary>
    /// When true, reads/writes wallet JSON under:
    /// Assets/StreamingAssets/Economy/{wallet.json,wallet_transactions.json}
    /// This is intended for editor-only testing since StreamingAssets may be treated as read-only in builds.
    /// </summary>
    public bool UseStreamingAssetsForEconomyFiles { get; set; } = true;

    /// <summary>
    /// Toggle this off if a caller needs silent wallet updates.
    /// </summary>
    public bool PublishWalletUpdatedEvents { get; set; } = true;

    public Wallet GetWallet(string playerId, bool createIfMissing = true)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            Debug.LogWarning("[EconomyService] GetWallet called with empty playerId.");
            return null;
        }

        var walletPath = GetEconomyFilePath(playerId, WalletFileName, UseStreamingAssetsForEconomyFiles);
        if (File.Exists(walletPath))
        {
            var wallet = ReadJsonFile<Wallet>(walletPath);
            if (wallet != null)
            {
                // Keep canonical player id aligned with request context.
                wallet.player_id = playerId;
                return wallet;
            }
        }

        // Backward compatibility with previous persistence layout (persistent data path, lowercase "economy").
        if (!UseStreamingAssetsForEconomyFiles)
        {
            var legacyWalletPath = FilePathResolver.GetEconomyPath(playerId, WalletFileName);
            if (File.Exists(legacyWalletPath))
            {
                var legacyWallet = ReadJsonFile<Wallet>(legacyWalletPath);
                if (legacyWallet != null)
                {
                    legacyWallet.player_id = playerId;
                    WriteJsonAtomic(walletPath, legacyWallet);
                    return legacyWallet;
                }
            }
        }

        if (!createIfMissing)
        {
            return null;
        }

        var created = CreateDefaultWallet(playerId);
        WriteJsonAtomic(walletPath, created);
        EnsureTransactionsFile(playerId);
        return created;
    }

    public bool TrySpend(string playerId, int coins, int gems, string source, out Wallet updatedWallet)
    {
        updatedWallet = GetWallet(playerId, true);
        if (updatedWallet == null)
        {
            return false;
        }

        if (coins < 0 || gems < 0)
        {
            Debug.LogWarning("[EconomyService] TrySpend rejected negative spend input.");
            return false;
        }

        if (coins == 0 && gems == 0)
        {
            return true;
        }

        if (updatedWallet.coins < coins || updatedWallet.gems < gems)
        {
            return false;
        }

        updatedWallet.coins -= coins;
        updatedWallet.gems -= gems;
        updatedWallet.last_updated = UtcNowIso();

        PersistWalletAndTransactions(
            playerId,
            updatedWallet,
            BuildTransactions(playerId, coins, gems, "spend", source));

        PublishWalletUpdated(playerId, source, updatedWallet);
        return true;
    }

    public void AddCurrency(string playerId, int coins, int gems, string source)
    {
        var wallet = GetWallet(playerId, true);
        if (wallet == null)
        {
            return;
        }

        if (coins < 0 || gems < 0)
        {
            Debug.LogWarning("[EconomyService] AddCurrency rejected negative input.");
            return;
        }

        if (coins == 0 && gems == 0)
        {
            return;
        }

        wallet.coins += coins;
        wallet.gems += gems;
        wallet.last_updated = UtcNowIso();

        PersistWalletAndTransactions(
            playerId,
            wallet,
            BuildTransactions(playerId, coins, gems, "earn", source));

        PublishWalletUpdated(playerId, source, wallet);
    }

    public IEnumerable<WalletTransaction> GetRecentTransactions(string playerId, int limit)
    {
        if (string.IsNullOrWhiteSpace(playerId) || limit <= 0)
        {
            return Enumerable.Empty<WalletTransaction>();
        }

        var transactionsPath = GetEconomyFilePath(playerId, TransactionsFileName, UseStreamingAssetsForEconomyFiles);
        if (!File.Exists(transactionsPath))
        {
            if (!UseStreamingAssetsForEconomyFiles)
            {
                var legacyPath = FilePathResolver.GetEconomyPath(playerId, TransactionsFileName);
                if (File.Exists(legacyPath))
                {
                    var legacyTransactions =
                        ReadJsonFile<List<WalletTransaction>>(legacyPath) ?? new List<WalletTransaction>();
                    WriteJsonAtomic(transactionsPath, legacyTransactions);

                    return legacyTransactions
                        .Where(t => t != null && t.player_id == playerId)
                        .OrderByDescending(t => ParseTimestampOrMin(t.timestamp))
                        .Take(limit)
                        .ToList();
                }
            }

            return Enumerable.Empty<WalletTransaction>();
        }

        var allTransactions = ReadJsonFile<List<WalletTransaction>>(transactionsPath) ?? new List<WalletTransaction>();

        return allTransactions
            .Where(t => t != null && t.player_id == playerId)
            .OrderByDescending(t => ParseTimestampOrMin(t.timestamp))
            .Take(limit)
            .ToList();
    }

    private static Wallet CreateDefaultWallet(string playerId)
    {
        return new Wallet
        {
            player_id = playerId,
            coins = 0,
            gems = 0,
            coaching_credits = 0,
            last_updated = UtcNowIso()
        };
    }

    private void EnsureTransactionsFile(string playerId)
    {
        var path = GetEconomyFilePath(playerId, TransactionsFileName, UseStreamingAssetsForEconomyFiles);
        if (File.Exists(path))
        {
            return;
        }

        WriteJsonAtomic(path, new List<WalletTransaction>());
    }

    private void PersistWalletAndTransactions(string playerId, Wallet wallet, List<WalletTransaction> newEntries)
    {
        var walletPath = GetEconomyFilePath(playerId, WalletFileName, UseStreamingAssetsForEconomyFiles);
        var transactionsPath = GetEconomyFilePath(playerId, TransactionsFileName, UseStreamingAssetsForEconomyFiles);

        var existingTransactions = ReadJsonFile<List<WalletTransaction>>(transactionsPath) ?? new List<WalletTransaction>();
        existingTransactions.AddRange(newEntries);

        WriteJsonAtomic(walletPath, wallet);
        WriteJsonAtomic(transactionsPath, existingTransactions);
    }

    private List<WalletTransaction> BuildTransactions(
        string playerId,
        int coins,
        int gems,
        string type,
        string source)
    {
        var list = new List<WalletTransaction>();
        var timestamp = UtcNowIso();
        var safeSource = string.IsNullOrWhiteSpace(source) ? "unknown_source" : source;

        if (coins > 0)
        {
            list.Add(new WalletTransaction
            {
                id = Guid.NewGuid().ToString(),
                player_id = playerId,
                amount = coins,
                currency = "coins",
                type = type,
                timestamp = timestamp,
                source = safeSource
            });
        }

        if (gems > 0)
        {
            list.Add(new WalletTransaction
            {
                id = Guid.NewGuid().ToString(),
                player_id = playerId,
                amount = gems,
                currency = "gems",
                type = type,
                timestamp = timestamp,
                source = safeSource
            });
        }

        return list;
    }

    private void PublishWalletUpdated(string playerId, string source, Wallet wallet)
    {
        if (!PublishWalletUpdatedEvents)
        {
            return;
        }

        var payload = new WalletUpdatedPayload
        {
            player_id = playerId,
            coins = wallet.coins,
            gems = wallet.gems,
            coaching_credits = wallet.coaching_credits,
            source = string.IsNullOrWhiteSpace(source) ? "unknown_source" : source,
            timestamp = UtcNowIso()
        };

        EventBus.Publish(new EventBus.EventEnvelope
        {
            event_type = "wallet_updated",
            player_id = playerId,
            payloadJson = JsonConvert.SerializeObject(payload)
        });
    }

    private static T ReadJsonFile<T>(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EconomyService] Failed to read JSON at '{path}': {ex.Message}");
            return default;
        }
    }

    private static void WriteJsonAtomic<T>(string destinationPath, T data)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = destinationPath + ".tmp";
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(tempPath, json);
        if (File.Exists(destinationPath))
        {
            var backupPath = destinationPath + ".bak";
            File.Replace(tempPath, destinationPath, backupPath, true);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            return;
        }

        File.Move(tempPath, destinationPath);
    }

    private static string UtcNowIso()
    {
        return DateTime.UtcNow.ToString("o");
    }

    private static DateTime ParseTimestampOrMin(string timestamp)
    {
        return DateTime.TryParse(timestamp, out var parsed) ? parsed : DateTime.MinValue;
    }

    private static string GetEconomyFilePath(string playerId, string fileName, bool useStreamingAssets)
    {
        if (useStreamingAssets)
        {
            var economyDir = Path.Combine(Application.dataPath, StreamingAssetsFolderName, EconomyFolderName);
            Directory.CreateDirectory(economyDir);
            return Path.Combine(economyDir, fileName);
        }

        var playerRoot = FilePathResolver.GetPlayerDataRoot(playerId);
        var persistentEconomyDir = Path.Combine(playerRoot, EconomyFolderName);
        Directory.CreateDirectory(persistentEconomyDir);
        return Path.Combine(persistentEconomyDir, fileName);
    }
}
