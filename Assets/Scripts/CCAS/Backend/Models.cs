using System;
using System.Collections.Generic;
using CCAS.Config;

namespace CCAS.Backend
{
    /// <summary>
    /// Result of a pack open operation.
    /// </summary>
    [Serializable]
    public class PackResult
    {
        public bool success;
        public string failureReason;   // "insufficient_funds" | "pack_not_found" | "catalog_error"
        public string packTypeId;
        public int costPaid;
        public List<Card> cards;
        public List<PackResultCard> cardDetails;

        public static PackResult Failure(string reason) =>
            new PackResult { success = false, failureReason = reason, cards = new(), cardDetails = new() };
    }

    [Serializable]
    public class PackResultCard
    {
        public string cardUid;
        public string rarity;
        public bool isDuplicate;
        public int xpAwarded;
    }

    /// <summary>
    /// One entry in the player's card collection.
    /// Stored in StreamingAssets/CCAS/card_collection.json (Phase 2 file I/O).
    /// </summary>
    [Serializable]
    public class CardCollectionEntry
    {
        public string player_id;
        public string card_id;
        public int quantity;
        public string first_acquired_at;   // ISO 8601
    }

    /// <summary>
    /// One entry in the pack drop history.
    /// Stored in StreamingAssets/CCAS/pack_drop_history.json (Phase 2 file I/O).
    /// </summary>
    [Serializable]
    public class PackDropHistoryEntry
    {
        public string id;              // GUID
        public string player_id;
        public string pack_type_id;
        public CostPaid cost_paid;
        public List<CardPull> cards_pulled;
        public string timestamp;       // ISO 8601
    }

    [Serializable]
    public class CostPaid
    {
        public int coins;
        public int gems;
    }

    [Serializable]
    public class CardPull
    {
        public string card_id;
        public string rarity;
        public bool is_duplicate;
        public int xp_awarded;
    }
}
