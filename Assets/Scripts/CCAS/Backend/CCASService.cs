using System.Collections.Generic;
using UnityEngine;
using CCAS.Backend;
using CCAS.Config;

/// <summary>
/// Central service for CCAS pack opening and collection management.
///
/// Phase 1 (this file):
///   - OpenPack: spends coins via EconomyService, rolls cards via DropConfigManager
///   - GetCollection: returns empty list (file I/O added in step 4.2)
///   - No XP awards yet (ProgressionService.AddXp wired in step 4.2)
///   - No persistence yet (step 4.2)
/// </summary>
public class CCASService : MonoBehaviour
{
    public static CCASService Instance;

    private readonly EconomyService _economy = new EconomyService();

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

        // Deduct coins via EconomyService
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

        var cardDetails = new List<PackResultCard>();
        foreach (var card in cards)
        {
            if (card == null) continue;
            // TODO (step 4.2): detect real duplicates from card_collection.json
            cardDetails.Add(new PackResultCard
            {
                cardUid     = card.uid ?? string.Empty,
                rarity      = card.GetRarityString(),
                isDuplicate = false,
                xpAwarded   = 0     // TODO (step 4.2): ProgressionService.AddXp for duplicates
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

        PublishPackEvents(playerId, packTypeId, packType.cost, cardDetails);

        return result;
    }

    void PublishPackEvents(string playerId, string packTypeId, int costCoins, List<PackResultCard> cardDetails)
    {
        // buy_pack — consumed by Economy + Progression listeners
        var cardEntries = new List<BuyPackCardEntry>();
        foreach (var detail in cardDetails)
        {
            cardEntries.Add(new BuyPackCardEntry
            {
                card_id      = detail.cardUid,
                rarity       = detail.rarity,
                is_duplicate = detail.isDuplicate,
                xp_awarded   = detail.xpAwarded
            });
        }

        EventBus.Publish(new EventBus.EventEnvelope
        {
            event_type  = "buy_pack",
            player_id   = playerId,
            payloadJson = JsonUtility.ToJson(new BuyPackPayload
            {
                pack_type_id = packTypeId,
                cost_paid    = new BuyPackCost { coins = costCoins, gems = 0 },
                cards_pulled = cardEntries
            })
        });

        // xp_from_duplicate — one event per duplicate card, consumed by Progression
        foreach (var entry in cardEntries)
        {
            if (!entry.is_duplicate) continue;
            EventBus.Publish(new EventBus.EventEnvelope
            {
                event_type  = "xp_from_duplicate",
                player_id   = playerId,
                payloadJson = JsonUtility.ToJson(new XpFromDuplicatePayload
                {
                    card_id      = entry.card_id,
                    rarity       = entry.rarity,
                    xp_gained    = entry.xp_awarded,
                    source       = $"duplicate_card_{entry.rarity}",
                    pack_type_id = packTypeId
                })
            });
        }
    }

    /// <summary>
    /// Returns the player's card collection.
    /// Phase 1: returns empty list. File I/O added in step 4.2.
    /// </summary>
    public IEnumerable<CardCollectionEntry> GetCollection(string playerId)
    {
        // TODO (step 4.2): load card_collection.json and filter by playerId
        return new List<CardCollectionEntry>();
    }

    [System.Serializable] private class BuyPackCost             { public int coins; public int gems; }
    [System.Serializable] private class BuyPackCardEntry        { public string card_id; public string rarity; public bool is_duplicate; public int xp_awarded; }
    [System.Serializable] private class BuyPackPayload          { public string pack_type_id; public BuyPackCost cost_paid; public List<BuyPackCardEntry> cards_pulled; }
    [System.Serializable] private class XpFromDuplicatePayload  { public string card_id; public string rarity; public int xp_gained; public string source; public string pack_type_id; }
}
