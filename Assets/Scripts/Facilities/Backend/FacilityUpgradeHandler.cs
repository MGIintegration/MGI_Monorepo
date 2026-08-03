using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FacilityUpgradeHandler : MonoBehaviour
{
    [Header("Set these in Inspector")]
    public string playerId = FacilitiesService.DefaultPlayerId;
    public string facilityTypeId; // weight_room, rehab_center, film_room
    public Button upgradeButton;

    private FacilitiesService facilitiesService;

    private void Awake()
    {
        facilitiesService = new FacilitiesService();
    }

    public bool OnUpgradeButtonClick()
    {
        Debug.Log($"Upgrade button clicked for facilityTypeId: {facilityTypeId}");

        if (upgradeButton != null)
            upgradeButton.interactable = false;

        bool success = facilitiesService.TryUpgradeFacility(playerId, facilityTypeId, out var newState);

        if (success)
        {
            Debug.Log($"Upgrade Successful: {facilityTypeId} is now level {newState.level}");

            var matchingHandlers = FindObjectsByType<FacilityDetailsHandler>(FindObjectsSortMode.None)
                .Where(h => h.playerFacilityId == facilityTypeId)
                .ToList();

            if (matchingHandlers.Count > 0)
            {
                foreach (var detailsHandler in matchingHandlers)
                {
                    detailsHandler.SetIds(playerId, facilityTypeId);
                    detailsHandler.RefreshFromLocalState();
                }
                Debug.Log($"Refreshed {matchingHandlers.Count} FacilityDetailsHandler instance(s) for {facilityTypeId}.");
            }
            else
            {
                Debug.LogWarning($"No FacilityDetailsHandler found for facilityTypeId '{facilityTypeId}'.");
            }
        }
        else
        {
            Debug.LogError($"Upgrade Failed for facilityTypeId: {facilityTypeId}");
        }

        if (upgradeButton != null)
            upgradeButton.interactable = true;

        return success;
    }
}