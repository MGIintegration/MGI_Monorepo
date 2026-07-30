using UnityEngine;
using UnityEngine.UI;

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

    void Start()
    {
        confirmButton.onClick.AddListener(HandleConfirm);
        cancelButton.onClick.AddListener(HandleCancel);
    }

    public void ShowModal()
    {
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
