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

        // Telemetry (with full card data)
        var packData = mgr.config.pack_types[packType];
        TelemetryLogger.Instance?.LogPull(
            packType,
            packData.name,  // replaced pack_id
            packData.cost,
            cards  // Pass Card objects instead of just rarities
        );

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
