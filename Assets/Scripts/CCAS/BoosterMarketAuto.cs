using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CCAS.Config;
using CCAS.Backend;

/// <summary>
/// Dynamically builds Booster Market UI from JSON config (phase1_simplified_config.json).
/// Clicking any pack opens it immediately.
/// </summary>
public class BoosterMarketAuto : MonoBehaviour
{
    [Header("UI Prefab + Layout")]
    public GameObject packButtonPrefab;
    public Transform contentParent;

    [Header("Optional UI Feedback")]
    [Tooltip("Assign the InsufficientFundsText TMP object on the Booster Market panel.")]
    public TMP_Text insufficientFundsText;
    [Tooltip("Seconds to show the insufficient funds message.")]
    public float insufficientFundsSeconds = 2f;

    private Coroutine _insufficientFundsRoutine;

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
            {
                // IMPORTANT: The prefab may have an OnClick wired in the Inspector (legacy path).
                // Clear it so opening always goes through CCASService.
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => TryOpenPack(packKey));
            }

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

        string playerId = PlayerPrefs.GetString("player_id", SystemInfo.deviceUniqueIdentifier);

        var service = CCASService.Instance;
        if (service == null)
        {
            Debug.LogError("[BoosterMarket] CCASService.Instance is missing in scene.");
            return;
        }

        var result = service.OpenPack(playerId, packKey);
        if (result == null || !result.success)
        {
            Debug.LogWarning($"[BoosterMarket] OpenPack failed: {result?.failureReason ?? "unknown"}");
            if (result != null && result.failureReason == "insufficient_funds")
                ShowInsufficientFunds();
            return;
        }

        Debug.Log($"[BoosterMarket] Opened pack via CCASService: {packKey}");
        hub.ShowPackOpening(packKey, result);
    }

    private void ShowInsufficientFunds()
    {
        if (insufficientFundsText == null)
            return;

        if (_insufficientFundsRoutine != null)
            StopCoroutine(_insufficientFundsRoutine);

        _insufficientFundsRoutine = StartCoroutine(ShowInsufficientFundsRoutine());
    }

    private IEnumerator ShowInsufficientFundsRoutine()
    {
        insufficientFundsText.gameObject.SetActive(true);
        yield return new WaitForSeconds(Mathf.Max(0.1f, insufficientFundsSeconds));
        insufficientFundsText.gameObject.SetActive(false);
        _insufficientFundsRoutine = null;
    }
}
