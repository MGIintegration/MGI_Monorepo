using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeModalController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private GameObject upgradeToastPanel;
    [SerializeField] private GameObject facilityDetailPanelToReturn;

    [Header("Toast Controller")]
    [SerializeField] private UpgradeToastController toastController;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Upgrade Logic")]
    [SerializeField] private FacilityUpgradeHandler upgradeHandler;

    [Header("Gate Message (reuses the modal's existing warning text)")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string defaultWarningText = "This action is permanent and affects player recovery.";

    [Header("Live Preview (was static per-room scene text; now filled in from config at ShowModal)")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text projectedBoostText;

    private readonly FacilitiesService _facilitiesService = new();

    void Start()
    {
        confirmButton.onClick.AddListener(HandleConfirm);
        cancelButton.onClick.AddListener(HandleCancel);
    }

    public void ShowModal()
    {
        bool canUpgrade = true;
        string blockReason = null;

        if (upgradeHandler != null)
        {
            canUpgrade = _facilitiesService.CanUpgradeFacility(
                upgradeHandler.playerId, upgradeHandler.facilityTypeId, out blockReason);
        }

        if (warningText != null)
        {
            warningText.text = canUpgrade ? defaultWarningText : blockReason;
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = canUpgrade;
        }

        if (upgradeHandler != null)
        {
            var preview = _facilitiesService.GetNextUpgradePreview(upgradeHandler.playerId, upgradeHandler.facilityTypeId);
            if (costText != null)
            {
                costText.text = preview != null ? $"Cost: ${preview.upgradeCost:n0}" : "-";
            }
            if (projectedBoostText != null)
            {
                projectedBoostText.text = preview != null
                    ? "Projected Boost:\n" + FacilityDetailsHandler.FormatWeeklyBoost(preview.benefits)
                    : "This facility is already at its maximum level.";
            }
        }

        modalPanel.SetActive(true);
        if (facilityDetailPanelToReturn != null)
            facilityDetailPanelToReturn.SetActive(false);
    }

    private void HandleConfirm()
    {
        bool success = upgradeHandler != null && upgradeHandler.OnUpgradeButtonClick();

        modalPanel.SetActive(false);

        if (success)
        {
            if (toastController != null)
                toastController.ShowToast("🏋️ Upgrade Complete: +1.3 STR/week!");
            else if (upgradeToastPanel != null)
                upgradeToastPanel.SetActive(true);
        }
        else if (facilityDetailPanelToReturn != null)
        {
            facilityDetailPanelToReturn.SetActive(true);
        }
    }

    private void HandleCancel()
    {
        modalPanel.SetActive(false);
        if (facilityDetailPanelToReturn != null)
            facilityDetailPanelToReturn.SetActive(true);
    }
}
