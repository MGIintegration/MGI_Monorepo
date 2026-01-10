using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CCAS.Config;

/// <summary>
/// Dynamically builds Booster Market UI from JSON config (phase1_simplified_config.json).
/// Clicking any pack opens it immediately.
/// </summary>
public class BoosterMarketAuto : MonoBehaviour
{
    [Header("UI Prefab + Layout")]
    public GameObject packButtonPrefab;
    public Transform contentParent;

    void Start()
    {
        GeneratePackButtons();
    }

    private void GeneratePackButtons()
    {
        var cfg = DropConfigManager.Instance?.config;
        if (cfg == null || cfg.pack_types == null)
        {
            Debug.LogError("[BoosterMarket] ❌ No config or pack_types found.");
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var kv in cfg.pack_types)
        {
            string packKey = kv.Key;
            PackType pack = kv.Value;

            var go = Instantiate(packButtonPrefab, contentParent);
            go.name = $"{pack.name}_Button";

            var buyButton = go.GetComponentInChildren<Button>(true);
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);

            if (label != null)
                label.text = $"{pack.name} ({pack.cost} coins)";

            if (buyButton != null)
                buyButton.onClick.AddListener(() => TryOpenPack(packKey));

            Debug.Log($"[BoosterMarket] Created button for {pack.name}");
        }
    }

    private void TryOpenPack(string packKey)
    {
        var hub = FindFirstObjectByType<AcquisitionHubController>();
        if (hub == null)
        {
            Debug.LogError("[BoosterMarket] No AcquisitionHubController found!");
            return;
        }

        Debug.Log($"[BoosterMarket] Opening pack: {packKey}");
        hub.ShowPackOpening(packKey);
    }
}
