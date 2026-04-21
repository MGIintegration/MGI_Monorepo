using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Central controller for navigation between all panels.
/// Ensures only one main panel (Hub, Market, PackOpening, History) is visible at a time.
/// </summary>
public class AcquisitionHubController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text coinsText;
    public TMP_Text xpText;

    [Header("Buttons")]
    public Button goToMarketButton;
    public Button myPacksButton;

    [Header("Panels")]
    public GameObject hubPanel;
    public GameObject marketPanel;
    public GameObject packPanel;
    public GameObject dropHistoryPanel;
    public GameObject myPacksPanel;

    private IDisposable _buyPackSub;

    void Start()
    {
        RefreshEconomyAndProgressionLabels();

        if (goToMarketButton != null)
            goToMarketButton.onClick.AddListener(ShowMarket);

        if (myPacksButton != null)
        {
            // Force-enable the Button component itself — scene serialization can leave it unchecked
            myPacksButton.enabled = true;
            myPacksButton.interactable = false;
            myPacksButton.onClick.AddListener(OnMyPacksClicked);
            Debug.Log("[CCAS] My Packs button found and wired up.");
        }
        else
        {
            Debug.LogWarning("[CCAS] myPacksButton is NULL in Start — assign it in the Inspector.");
        }

        // Seed button state from previous sessions
        RefreshMyPacksButton("Start");

        // If the panel isn't assigned in the scene, keep the button disabled to avoid spam.
        if (myPacksPanel == null && myPacksButton != null)
        {
            myPacksButton.interactable = false;
        }

        // Subscribe to buy_pack event — fires every time a pack is opened
        _buyPackSub = EventBus.Subscribe("buy_pack", env =>
        {
            Debug.Log($"[CCAS] buy_pack event received (player={env.player_id}) → enabling My Packs button");
            PlayerPrefs.SetInt("ccas_has_pack_history", 1);
            PlayerPrefs.Save();
            RefreshMyPacksButton("buy_pack event");
        });

        ShowHub();
    }

    void OnDestroy()
    {
        _buyPackSub?.Dispose();
    }

    private void OnMyPacksClicked()
    {
        Debug.Log($"[CCAS] My Packs button clicked. myPacksPanel={(myPacksPanel != null ? myPacksPanel.name : "NULL")}");

        if (myPacksPanel != null)
            ShowMyPacks();
        // else: button should be non-interactable when myPacksPanel is null
    }

    // --- Navigation Helpers ---
    public void ShowHub()
    {
        SetActivePanel(hubPanel);
        Canvas.ForceUpdateCanvases();
        if (hubPanel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(hubPanel.GetComponent<RectTransform>());
        RefreshEconomyAndProgressionLabels();
        RefreshMyPacksButton("ShowHub");
    }
    public void ShowMarket() => SetActivePanel(marketPanel);
    public void ShowMyPacks() => SetActivePanel(myPacksPanel);

    public void ShowPackOpening(string packKey)
    {
        SetActivePanel(packPanel);

        var opener = packPanel?.GetComponentInChildren<PackOpeningController>(true);
        if (opener != null)
            opener.OpenPackOfType(packKey);
    }

    public void ShowPackOpening(string packKey, CCAS.Backend.PackResult result)
    {
        SetActivePanel(packPanel);

        var opener = packPanel?.GetComponentInChildren<PackOpeningController>(true);
        if (opener != null)
            opener.OpenPackOfType(packKey, result);
    }

    public void ShowHistory()
    {
        SetActivePanel(dropHistoryPanel);
    }

    private void SetActivePanel(GameObject active)
    {
        hubPanel?.SetActive(active == hubPanel);
        marketPanel?.SetActive(active == marketPanel);
        packPanel?.SetActive(active == packPanel);
        dropHistoryPanel?.SetActive(active == dropHistoryPanel);
        if (myPacksPanel != null)
            myPacksPanel.SetActive(active == myPacksPanel);
    }

    /// <summary>
    /// Enables My Packs button when the player has opened at least one pack.
    /// Checks PlayerPrefs flag first (fast), falls back to TelemetryLogger history.
    /// </summary>
    private void RefreshMyPacksButton(string caller)
    {
        if (myPacksButton == null)
        {
            Debug.LogWarning($"[CCAS] RefreshMyPacksButton({caller}): myPacksButton is NULL");
            return;
        }

        bool hasHistory = PlayerPrefs.GetInt("ccas_has_pack_history", 0) == 1;

        if (!hasHistory)
        {
            var recent = TelemetryLogger.Instance?.GetRecent(1);
            hasHistory = recent != null && recent.Count > 0;
            if (hasHistory)
            {
                PlayerPrefs.SetInt("ccas_has_pack_history", 1);
                PlayerPrefs.Save();
            }
        }

        myPacksButton.interactable = hasHistory;
        Debug.Log($"[CCAS] RefreshMyPacksButton({caller}): hasHistory={hasHistory} → interactable={myPacksButton.interactable}");
    }

    /// <summary>
    /// Updates the XP label on the hub using the player's total duplicate XP.
    /// </summary>
    private void RefreshEconomyAndProgressionLabels()
    {
        string playerId = PlayerPrefs.GetString("player_id", SystemInfo.deviceUniqueIdentifier);

        if (coinsText != null)
        {
            var wallet = new EconomyService().GetWallet(playerId, true);
            coinsText.text = wallet != null ? $"Coins: {wallet.coins}" : "Coins: ?";
        }

        if (xpText != null)
        {
            var prog = ProgressionService.Instance;
            var state = prog != null ? prog.GetState(playerId, true) : null;
            xpText.text = state != null
                ? $"XP: {state.current_xp}"
                : $"XP: {PlayerPrefs.GetInt("player_xp", 0)}";
        }
    }
}
