using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using CCAS.Config;

/// <summary>
/// Loads phase1_config.json from StreamingAssets
/// and handles randomized card pulls.
/// </summary>
public class DropConfigManager : MonoBehaviour
{
    public static DropConfigManager Instance;

    [Header("Runtime Config (Loaded JSON)")]
    public CCASConfigRoot config;   // maps directly to json

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadConfig();
    }

    /// <summary>
    /// Loads Phase 2 config from StreamingAssets/CCAS.
    /// </summary>
    void LoadConfig()
    {
        // Phase 2: config file renamed (derived from phase1_config.json).
        // See: Assets/StreamingAssets/CCAS/phase2_config.json
        string path = Path.Combine(Application.streamingAssetsPath, "CCAS", "phase2_config.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"[DropConfig] ❌ Config not found at {path}");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                FloatParseHandling = FloatParseHandling.Double
            };

            config = JsonConvert.DeserializeObject<CCASConfigRoot>(json, settings);

            if (config?.pack_types != null && config.rarity_values != null)
            {
                Debug.Log($"[DropConfig] ✅ Loaded {config.pack_types.Count} pack types and {config.rarity_values.Count} rarity tiers from phase2_config.json");
            }
            else
            {
                Debug.LogError("[DropConfig] ❌ Missing pack_types or rarity_values in phase2_config.json");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DropConfig] ❌ Failed to load config: {e.Message}");
        }
    }

    /// <summary>
    /// Simulates card pulls for a given pack type and returns a list of rarity strings.
    /// </summary>
    public List<string> PullCardRarities(string packKey)
    {

        var results = new List<string>();

        if (config == null || config.pack_types == null || !config.pack_types.ContainsKey(packKey))
        {
            Debug.LogError($"[DropConfig] Pack type not found: {packKey}");
            return results;
        }

        var pack = config.pack_types[packKey];
        var rates = pack.drop_rates;
        int pulls = Mathf.Max(1, pack.guaranteed_cards);

        for (int i = 0; i < pulls; i++)
        {
            string rarity = WeightedRoll(rates);
            results.Add(rarity);
        }
        return results;
    }

    /// <summary>
    /// Simulates card pulls for a given pack type and returns a list of actual Card objects.
    /// Uses CardCatalogLoader to select cards based on the pulled rarities.
    /// </summary>
    public List<Card> PullCards(string packKey)
    {
        var results = new List<Card>();

        if (config == null || config.pack_types == null || !config.pack_types.ContainsKey(packKey))
        {
            Debug.LogError($"[DropConfig] Pack type not found: {packKey}");
            return results;
        }

        var catalogLoader = CardCatalogLoader.Instance;
        if (catalogLoader == null)
        {
            Debug.LogError("[DropConfig] CardCatalogLoader.Instance is null. Cannot pull cards.");
            return results;
        }

        var pack = config.pack_types[packKey];
        var rates = pack.drop_rates;
        int pulls = Mathf.Max(1, pack.guaranteed_cards);

        for (int i = 0; i < pulls; i++)
        {
            string rarity = WeightedRoll(rates);
            Card card = catalogLoader.GetRandomCardByRarity(rarity);
            if (card != null)
            {
                results.Add(card);
            }
            else
            {
                Debug.LogWarning($"[DropConfig] Failed to get card for rarity: {rarity}");
            }
        }

        return results;
    }

    // ---------- Helper Methods ----------

    string WeightedRoll(DropRates rates)
    {
        float c  = Mathf.Max(0f, rates.common);
        float uc = Mathf.Max(0f, rates.uncommon);
        float r  = Mathf.Max(0f, rates.rare);
        float e  = Mathf.Max(0f, rates.epic);
        float l  = Mathf.Max(0f, rates.legendary);

        float total = c + uc + r + e + l;
        if (total <= 0f) return "common";

        float roll = UnityEngine.Random.value * total;
        if ((roll -= c)  < 0f) return "common";
        if ((roll -= uc) < 0f) return "uncommon";
        if ((roll -= r)  < 0f) return "rare";
        if ((roll -= e)  < 0f) return "epic";
        return "legendary";
    }
}
