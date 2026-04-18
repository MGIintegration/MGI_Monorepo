using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PackOpeningController : MonoBehaviour
{
    [Header("Navigation")]
    public Button continueButton;
    public GameObject packPanel;
    public GameObject dropHistoryPanel;

    [Header("Card UI")]
    public Transform cardParent;
    public GameObject cardPrefab;

    [Header("Settings")]
    public string packType = "bronze_pack";

    [Header("References")]
    public DropHistoryController dropHistoryController;

    private readonly List<GameObject> _cards = new();

    void Start()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() =>
            {

                FindFirstObjectByType<AcquisitionHubController>()?.ShowHistory();
                dropHistoryController?.RefreshDropHistory();

            });
        }
    }

    public void OpenPackOfType(string key)
    {
        packType = key;
        OpenPack();
    }

    [ContextMenu("Open Pack (Current Setting)")]
    public void OpenPack()
    {
        if (string.IsNullOrEmpty(packType))
        {
            Debug.LogWarning("[PackOpening] packType not set.");
            return;
        }
        if (cardParent == null || cardPrefab == null)
        {
            Debug.LogError("[PackOpening] Missing references: cardParent or cardPrefab.");
            return;
        }

        var mgr = DropConfigManager.Instance;
        if (mgr == null || mgr.config == null)
        {
            Debug.LogError("[PackOpening] DropConfigManager/config missing.");
            return;
        }

        // Pull actual Card objects instead of just rarity strings
        var cards = mgr.PullCards(packType);
        Debug.Log($"[PackOpening] {packType} → {cards.Count} cards");

        if (cards.Count == 0)
        {
            Debug.LogWarning("[PackOpening] No cards were pulled. Falling back to rarity-only display.");
            // Fallback to old system for compatibility
            var rarities = mgr.PullCardRarities(packType);
            BuildOrReuseCards(rarities.Count);
            for (int i = 0; i < _cards.Count; i++)
            {
                var go = _cards[i];
                if (i < rarities.Count)
                {
                    go.SetActive(true);
                    SetCard(go, rarities[i]);
                }
                else go.SetActive(false);
            }
            return;
        }

        BuildOrReuseCards(cards.Count);

        // Extract rarities for emotion/hooks/telemetry (backward compatibility)
        var raritiesForHooks = new List<string>();
        foreach (var card in cards)
        {
            raritiesForHooks.Add(card.GetRarityString());
        }

        // Emotion + hooks
        EmotionalStateManager.Instance?.ApplyPackOutcome(packType, raritiesForHooks);
        HookOrchestrator.Instance?.TryTriggerOutcomeHooks(raritiesForHooks);

        var packData = mgr.config.pack_types[packType];

        // Telemetry (with full card data)
        TelemetryLogger.Instance?.LogPull(
            packType,
            packData.name,
            packData.cost,
            cards
        );

        // Events last (ensure Telemetry has saved the data before subscribers refresh)
        PublishBuyPackEvent(packType, packData.cost, cards);

        // Visuals - display actual card information
        for (int i = 0; i < _cards.Count; i++)
        {
            var go = _cards[i];
            if (i < cards.Count)
            {
                go.SetActive(true);
                SetCard(go, cards[i]);
            }
            else go.SetActive(false);
        }

        // UI
        if (packPanel) packPanel.SetActive(true);
        if (dropHistoryPanel) dropHistoryPanel.SetActive(false);

        if (cardParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    void PublishBuyPackEvent(string packTypeKey, int costCoins, List<Card> cards)
    {
        string playerId = PlayerPrefs.GetString("player_id", SystemInfo.deviceUniqueIdentifier);

        // Read duplicate history before this pull — same snapshot TelemetryLogger uses
        var pullCounts = TelemetryLogger.Instance?.BuildCardPullCountsFromHistory()
                         ?? new Dictionary<string, int>();

        var cardEntries = new List<BuyPackCardEntry>();
        foreach (var card in cards)
        {
            if (card == null) continue;
            string uid    = card.uid ?? string.Empty;
            string rarity = card.GetRarityString();

            int prevPulls = 0;
            if (!string.IsNullOrEmpty(uid)) pullCounts.TryGetValue(uid, out prevPulls);
            bool isDuplicate = !string.IsNullOrEmpty(uid) && prevPulls > 0;
            if (!string.IsNullOrEmpty(uid)) pullCounts[uid] = prevPulls + 1;

            int xp = isDuplicate
                ? (TelemetryLogger.Instance?.GetDuplicateXpForRarity(rarity) ?? 0)
                : 0;

            cardEntries.Add(new BuyPackCardEntry
            {
                card_id      = uid,
                rarity       = rarity,
                is_duplicate = isDuplicate,
                xp_awarded   = xp
            });
        }

        // 1. buy_pack — consumed by Economy + Progression when integrated
        EventBus.Publish(new EventBus.EventEnvelope
        {
            event_type  = "buy_pack",
            player_id   = playerId,
            payloadJson = JsonUtility.ToJson(new BuyPackPayload
            {
                pack_type_id = packTypeKey,
                cost_paid    = new CostPaid { coins = costCoins, gems = 0 },
                cards_pulled = cardEntries
            })
        });

        // 2. xp_from_duplicate — one event per duplicate card, consumed by Progression
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
                    pack_type_id = packTypeKey
                })
            });
        }
    }

    [System.Serializable] private class CostPaid            { public int coins; public int gems; }
    [System.Serializable] private class BuyPackCardEntry    { public string card_id; public string rarity; public bool is_duplicate; public int xp_awarded; }
    [System.Serializable] private class BuyPackPayload      { public string pack_type_id; public CostPaid cost_paid; public List<BuyPackCardEntry> cards_pulled; }
    [System.Serializable] private class XpFromDuplicatePayload { public string card_id; public string rarity; public int xp_gained; public string source; public string pack_type_id; }

    void BuildOrReuseCards(int needed)
    {
        while (_cards.Count < needed)
        {
            var go = Instantiate(cardPrefab, cardParent);
            go.SetActive(false);
            _cards.Add(go);
        }
    }

    void SetCard(GameObject go, string rarityRaw)
    {
        string rarity = (rarityRaw ?? "common").ToLowerInvariant();
        var img = go.GetComponentInChildren<Image>(true);
        if (img != null) img.color = RarityColorUtility.GetColorForRarity(rarity);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) tmp.text = rarity.ToUpperInvariant();
    }

    /// <summary>
    /// Sets card UI with actual Card data (Name, Team, Element, Position5).
    /// </summary>
    void SetCard(GameObject go, Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("[PackOpening] Attempted to set null card");
            return;
        }

        // Set card color based on rarity
        var img = go.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            string rarity = card.GetRarityString();
            img.color = RarityColorUtility.GetColorForRarity(rarity);
        }

        // Try to use CardView component if available
        var cardView = go.GetComponent<CardView>();
        if (cardView != null)
        {
            cardView.Apply(card);
        }
        else
        {
            // Fallback: set text directly if CardView not available
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                // Display card name as primary text
                tmp.text = card.name;
            }
        }
    }

}
