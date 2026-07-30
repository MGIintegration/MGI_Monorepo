using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubsystemResetMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetEconomyButton;
    [SerializeField] private Button resetFacilitiesButton;
    [SerializeField] private Button resetCoachesButton;
    [SerializeField] private Button resetCCASButton;
    [SerializeField] private Button addCoinsButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Coin Settings")]
    [SerializeField] private int coinsToAdd = 10000;

    [Header("Player")]
    [SerializeField] private string playerId = "local_player";

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (!string.IsNullOrWhiteSpace(playerId))
        {
            PlayerPrefs.SetString("player_id", playerId);
            PlayerPrefs.Save();
        }

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (resetEconomyButton != null) resetEconomyButton.onClick.AddListener(ResetEconomy);
        if (resetFacilitiesButton != null) resetFacilitiesButton.onClick.AddListener(ResetFacilities);
        if (resetCoachesButton != null) resetCoachesButton.onClick.AddListener(ResetCoaches);
        if (resetCCASButton != null) resetCCASButton.onClick.AddListener(ResetCCAS);
        if (addCoinsButton != null) addCoinsButton.onClick.AddListener(AddCoins);
    }

    private void Open()
    {
        if (panel != null) panel.SetActive(true);
        SetStatus("Select a subsystem to reset.");
    }

    private void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void ResetEconomy()
    {
        var service = new EconomyService();
        bool ok = service.ResetWallet(playerId);
        SetStatus(ok ? "Economy wallet reset." : "Economy reset failed.");
    }

    private void ResetFacilities()
    {
        var service = new FacilitiesService();
        bool ok = service.ResetFacilityState(playerId);
        SetStatus(ok ? "Facilities state reset." : "Facilities reset failed.");
    }

    private void ResetCoaches()
    {
        bool ok = CoachesService.ResetPlayerCoachState(playerId);
        SetStatus(ok ? "Coaches state reset." : "No coach state to reset.");
    }

    private void ResetCCAS()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.ResetWallet();
            SetStatus("CCAS wallet reset.");
            return;
        }

        SetStatus("CCAS wallet reset failed.");
    }

    private void AddCoins()
    {
        var service = new EconomyService();
        service.AddCurrency(playerId, coinsToAdd, 0, 0, "menu_add_coins");

        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.AddCoins(coinsToAdd);
        }

        var hub = FindObjectOfType<AcquisitionHubController>();
        hub?.RefreshEconomyAndProgressionLabels();

        SetStatus($"Added {coinsToAdd:N0} coins to the economy and CCAS wallets.");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
