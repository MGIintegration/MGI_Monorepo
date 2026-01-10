using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Loads cards_catalog.json from StreamingAssets and provides access to card data.
/// </summary>
public class CardCatalogLoader : MonoBehaviour
{
    public static CardCatalogLoader Instance;

    [Header("Runtime Data (Loaded JSON)")]
    public CardsCatalog catalog;

    private Dictionary<int, List<Card>> _cardsByTier;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCatalog();
    }

    /// <summary>
    /// Loads cards_catalog.json from StreamingAssets.
    /// </summary>
    void LoadCatalog()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "cards_catalog.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"[CardCatalog] ❌ Catalog not found at {path}");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            };

            catalog = JsonConvert.DeserializeObject<CardsCatalog>(json, settings);

            if (catalog?.cards != null && catalog.cards.Length > 0)
            {
                BuildTierIndex();
                Debug.Log($"[CardCatalog] ✅ Loaded {catalog.cards.Length} cards from cards_catalog.json");
            }
            else
            {
                Debug.LogError("[CardCatalog] ❌ No cards found in catalog");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CardCatalog] ❌ Failed to load catalog: {e.Message}");
        }
    }

    /// <summary>
    /// Builds an index of cards grouped by tier for fast lookup.
    /// </summary>
    void BuildTierIndex()
    {
        _cardsByTier = new Dictionary<int, List<Card>>();

        foreach (var card in catalog.cards)
        {
            if (!_cardsByTier.ContainsKey(card.cardTier))
            {
                _cardsByTier[card.cardTier] = new List<Card>();
            }
            _cardsByTier[card.cardTier].Add(card);
        }

        Debug.Log($"[CardCatalog] Indexed cards by tier: {string.Join(", ", _cardsByTier.Keys.Select(k => $"Tier {k}: {_cardsByTier[k].Count}"))}");
    }

    /// <summary>
    /// Gets a random card of the specified tier.
    /// </summary>
    public Card GetRandomCardByTier(int tier)
    {
        if (_cardsByTier == null || !_cardsByTier.ContainsKey(tier) || _cardsByTier[tier].Count == 0)
        {
            Debug.LogWarning($"[CardCatalog] No cards found for tier {tier}, falling back to tier 1");
            tier = 1;
            if (!_cardsByTier.ContainsKey(tier) || _cardsByTier[tier].Count == 0)
            {
                Debug.LogError("[CardCatalog] No cards available at all!");
                return null;
            }
        }

        var cards = _cardsByTier[tier];
        return cards[UnityEngine.Random.Range(0, cards.Count)];
    }

    /// <summary>
    /// Gets a random card by rarity string (for compatibility with existing system).
    /// </summary>
    public Card GetRandomCardByRarity(string rarity)
    {
        int tier = Card.RarityStringToTier(rarity);
        return GetRandomCardByTier(tier);
    }

    /// <summary>
    /// Gets all cards of a specific tier.
    /// </summary>
    public List<Card> GetCardsByTier(int tier)
    {
        if (_cardsByTier == null || !_cardsByTier.ContainsKey(tier))
        {
            return new List<Card>();
        }
        return new List<Card>(_cardsByTier[tier]);
    }
}

