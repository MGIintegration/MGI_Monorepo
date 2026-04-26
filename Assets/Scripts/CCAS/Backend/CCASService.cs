using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using CCAS.Backend;
using CCAS.Config;

/// <summary>
/// Central service for CCAS pack opening and collection management.
/// Implements end-to-end pack opening:
/// - Spend via EconomyService before rolling
/// - Persist card collection + pack drop history under FilePathResolver CCAS paths
/// - Detect duplicates from persisted collection
/// - Award XP for duplicates via ProgressionService (idempotent)
/// - Publish buy_pack event through EventBus
/// </summary>
public class CCASService : MonoBehaviour
{
    public static CCASService Instance;

    private readonly EconomyService _economy = new EconomyService();

    private const string CardCollectionFileName = "card_collection.json";
    private const string PackDropHistoryFileName = "pack_drop_history.json";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Opens a pack for the given player: spends coins, rolls cards, returns PackResult.
    /// </summary>
    public PackResult OpenPack(string playerId, string packTypeId)
    {
        var mgr = DropConfigManager.Instance;
        if (mgr == null || mgr.config == null)
        {
            Debug.LogError("[CCASService] DropConfigManager not available.");
            return PackResult.Failure("catalog_error");
        }

        if (!mgr.config.pack_types.TryGetValue(packTypeId, out var packType))
        {
            Debug.LogError($"[CCASService] Pack type not found: {packTypeId}");
            return PackResult.Failure("pack_not_found");
        }

        if (string.IsNullOrWhiteSpace(playerId))
        {
            Debug.LogWarning("[CCASService] OpenPack called with empty playerId.");
            return PackResult.Failure("invalid_player");
        }

        // Spend via EconomyService first (authoritative)
        if (!_economy.TrySpend(playerId, packType.cost, 0, "pack_purchase"))
        {
            Debug.LogWarning($"[CCASService] Insufficient funds for player {playerId} buying {packTypeId}");
            return PackResult.Failure("insufficient_funds");
        }

        var cards = mgr.PullCards(packTypeId);
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning($"[CCASService] PullCards returned empty for {packTypeId}");
            return PackResult.Failure("catalog_error");
        }

        var nowIso = DateTime.UtcNow.ToString("o");
        var packOpenId = Guid.NewGuid().ToString();

        // Load existing collection for duplicate detection
        var collection = LoadCardCollection(playerId);
        var existingByCardId = collection.entries
            .Where(e => e != null && e.player_id == playerId && !string.IsNullOrEmpty(e.card_id))
            .GroupBy(e => e.card_id)
            .ToDictionary(g => g.Key, g => g.First());

        var cardDetails = new List<PackResultCard>();
        var cardsPulledForHistory = new List<CardPull>();

        foreach (var card in cards)
        {
            if (card == null) continue;

            var cardId = card.uid ?? string.Empty;
            var rarity = card.GetRarityString();

            bool isDuplicate = !string.IsNullOrEmpty(cardId) &&
                               existingByCardId.TryGetValue(cardId, out var existingEntry) &&
                               existingEntry != null &&
                               existingEntry.quantity > 0;

            int xpAwarded = 0;
            if (isDuplicate)
            {
                xpAwarded = GetDuplicateXpForRarity(mgr.config, rarity);
                var progression = ProgressionService.Instance;
                if (progression != null && xpAwarded > 0)
                {
                    var eventId = $"ccas_pack_open:{packOpenId}:{cardId}";
                    progression.AddXp(playerId, xpAwarded, $"duplicate_card_{rarity}", eventId);
                }
            }

            // Update collection quantities (duplicates still increment quantity)
            if (!string.IsNullOrEmpty(cardId))
            {
                if (!existingByCardId.TryGetValue(cardId, out var entry) || entry == null)
                {
                    entry = new CardCollectionEntry
                    {
                        player_id = playerId,
                        card_id = cardId,
                        quantity = 0,
                        first_acquired_at = nowIso
                    };
                    collection.entries.Add(entry);
                    existingByCardId[cardId] = entry;
                }

                entry.quantity = Mathf.Max(0, entry.quantity) + 1;
                if (string.IsNullOrWhiteSpace(entry.first_acquired_at))
                    entry.first_acquired_at = nowIso;
            }

            cardDetails.Add(new PackResultCard
            {
                cardUid = cardId,
                rarity = rarity,
                isDuplicate = isDuplicate,
                xpAwarded = xpAwarded
            });

            cardsPulledForHistory.Add(new CardPull
            {
                card_id = cardId,
                rarity = rarity,
                is_duplicate = isDuplicate,
                xp_awarded = xpAwarded
            });
        }

        Debug.Log($"[CCASService] OpenPack({packTypeId}) → {cards.Count} cards for player {playerId}");

        var result = new PackResult
        {
            success     = true,
            packTypeId  = packTypeId,
            costPaid    = packType.cost,
            cards       = cards,
            cardDetails = cardDetails
        };

        // Persist state after pull
        SaveCardCollection(playerId, collection);
        AppendPackDropHistory(playerId, new PackDropHistoryEntry
        {
            id = Guid.NewGuid().ToString(),
            player_id = playerId,
            pack_type_id = packTypeId,
            cost_paid = new CostPaid { coins = packType.cost, gems = 0 },
            cards_pulled = cardsPulledForHistory,
            timestamp = nowIso
        });

        PublishBuyPackEvent(playerId, packTypeId, packType.cost, cardsPulledForHistory);

        return result;
    }

    private void PublishBuyPackEvent(string playerId, string packTypeId, int costCoins, List<CardPull> cardsPulled)
    {
        EventBus.Publish(new EventBus.EventEnvelope
        {
            event_type  = "buy_pack",
            player_id   = playerId,
            payloadJson = JsonUtility.ToJson(new BuyPackPayload
            {
                pack_type_id = packTypeId,
                cost_paid = new BuyPackCost { coins = costCoins, gems = 0 },
                cards_pulled = cardsPulled.Select(p => new BuyPackCardEntry
                {
                    card_id = p.card_id,
                    rarity = p.rarity,
                    is_duplicate = p.is_duplicate,
                    xp_awarded = p.xp_awarded
                }).ToList()
            })
        });
    }

    /// <summary>
    /// Returns the player's card collection.
    /// Phase 1: returns empty list. File I/O added in step 4.2.
    /// </summary>
    public IEnumerable<CardCollectionEntry> GetCollection(string playerId)
    {
        var collection = LoadCardCollection(playerId);
        return collection.entries.Where(e => e != null && e.player_id == playerId).ToList();
    }

    [System.Serializable] private class BuyPackCost             { public int coins; public int gems; }
    [System.Serializable] private class BuyPackCardEntry        { public string card_id; public string rarity; public bool is_duplicate; public int xp_awarded; }
    [System.Serializable] private class BuyPackPayload          { public string pack_type_id; public BuyPackCost cost_paid; public List<BuyPackCardEntry> cards_pulled; }

    [Serializable]
    private class CardCollectionFile
    {
        public List<CardCollectionEntry> entries = new();
    }

    [Serializable]
    private class PackDropHistoryFile
    {
        public List<PackDropHistoryEntry> entries = new();
    }

    private static int GetDuplicateXpForRarity(CCASConfigRoot cfg, string rarity)
    {
        var dxp = cfg?.duplicate_xp;
        var r = (rarity ?? "common").ToLowerInvariant();

        if (dxp == null)
        {
            return r switch
            {
                "uncommon" => 10,
                "rare" => 25,
                "epic" => 50,
                "legendary" => 100,
                _ => 5
            };
        }

        return r switch
        {
            "uncommon" => dxp.uncommon_duplicate_xp,
            "rare" => dxp.rare_duplicate_xp,
            "epic" => dxp.epic_duplicate_xp,
            "legendary" => dxp.legendary_duplicate_xp,
            _ => dxp.common_duplicate_xp
        };
    }

    private static CardCollectionFile LoadCardCollection(string playerId)
    {
        var path = FilePathResolver.GetCCASPath(playerId, CardCollectionFileName);
        if (!File.Exists(path))
            return new CardCollectionFile();

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonConvert.DeserializeObject<CardCollectionFile>(json);
            return file ?? new CardCollectionFile();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CCASService] Failed to read card collection: {ex.Message}");
            return new CardCollectionFile();
        }
    }

    private static void SaveCardCollection(string playerId, CardCollectionFile file)
    {
        var path = FilePathResolver.GetCCASPath(playerId, CardCollectionFileName);
        WriteJsonAtomic(path, file ?? new CardCollectionFile());
    }

    private static void AppendPackDropHistory(string playerId, PackDropHistoryEntry entry)
    {
        if (entry == null) return;

        var path = FilePathResolver.GetCCASPath(playerId, PackDropHistoryFileName);
        PackDropHistoryFile existing;

        if (File.Exists(path))
        {
            try
            {
                existing = JsonConvert.DeserializeObject<PackDropHistoryFile>(File.ReadAllText(path)) ?? new PackDropHistoryFile();
            }
            catch
            {
                existing = new PackDropHistoryFile();
            }
        }
        else
        {
            existing = new PackDropHistoryFile();
        }

        existing.entries.Add(entry);
        WriteJsonAtomic(path, existing);
    }

    private static void WriteJsonAtomic<T>(string destinationPath, T data)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = destinationPath + ".tmp";
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(tempPath, json);

        if (File.Exists(destinationPath))
        {
            var backupPath = destinationPath + ".bak";
            File.Replace(tempPath, destinationPath, backupPath, true);
            if (File.Exists(backupPath))
                File.Delete(backupPath);
            return;
        }

        File.Move(tempPath, destinationPath);
    }
}
