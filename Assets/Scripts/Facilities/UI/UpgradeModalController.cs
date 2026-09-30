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

    // Short first-line summary of the boost being confirmed, cached from ShowModal's preview
    // so HandleConfirm's toast can name the room's actual new benefit instead of a fixed string.
    private string _pendingBoostSummary;

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

            _pendingBoostSummary = preview != null
                ? StripBulletPrefix(FacilityDetailsHandler.FormatWeeklyBoost(preview.benefits).Split('\n')[0])
                : null;
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
            string toastMessage = _pendingBoostSummary != null
                ? $"✅ Upgrade Complete: {_pendingBoostSummary}!"
                : "✅ Upgrade Complete!";

            if (toastController != null)
                toastController.ShowToast(toastMessage);
            else if (upgradeToastPanel != null)
                upgradeToastPanel.SetActive(true);
        }
        else if (facilityDetailPanelToReturn != null)
        {
            facilityDetailPanelToReturn.SetActive(true);
        }
    }

    // FormatWeeklyBoost returns bullet lines ("- +16% STR"); this drops the "- " for a
    // one-line toast. The InjuryTimeReductionWeeks special case in FormatWeeklyBoost has
    // no bullet prefix and is returned unchanged (not currently reachable via the live
    // config, since no facility's benefits use that key today, but kept safe either way).
    private static string StripBulletPrefix(string line)
    {
        return line.StartsWith("- ") ? line.Substring(2) : line;
    }

    private void HandleCancel()
    {
        modalPanel.SetActive(false);
        if (facilityDetailPanelToReturn != null)
            facilityDetailPanelToReturn.SetActive(true);
    }
}
