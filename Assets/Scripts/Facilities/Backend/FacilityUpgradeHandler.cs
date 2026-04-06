using UnityEngine;
using UnityEngine.UI;

public class FacilityUpgradeHandler : MonoBehaviour
{
    [Header("Set these in Inspector")]
    public string teamId;
    public string playerFacilityId;
    public string action; // "start", "confirm", "rollback"
    public Button upgradeButton;   // optional: to disable/enable during request

    private FacilitiesService facilitiesService;

    private void Awake()
    {
        facilitiesService = new FacilitiesService();
    }

    // Hook this in the Button's OnClick()
    public void OnUpgradeButtonClick()
    {
        Debug.Log($"Upgrade button clicked! Action: {action}");

        if (upgradeButton != null)
            upgradeButton.interactable = false;

        var result = facilitiesService.TryUpgradeFacility(teamId, playerFacilityId, action);

        if (result.Success)
        {
            Debug.Log("Upgrade Successful: " + result.Message);

            var detailsHandler = FindObjectOfType<FacilityDetailsHandler>();
            if (detailsHandler != null)
            {
                detailsHandler.SetIds(teamId, playerFacilityId);
                detailsHandler.RefreshFromServer();
                Debug.Log("FacilityDetailsHandler refreshed after upgrade.");
            }
            else
            {
                Debug.LogWarning("FacilityDetailsHandler not found in scene.");
            }
        }
        else
        {
            Debug.LogError("Upgrade Failed: " + result.Message);
        }

        if (upgradeButton != null)
            upgradeButton.interactable = true;
    }
}