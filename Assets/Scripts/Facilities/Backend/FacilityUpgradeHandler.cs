using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FacilityUpgradeHandler : MonoBehaviour
{
    [Header("Set these in Inspector")]
    public string teamId;
    public string playerFacilityId;
    public string action; // "start", "confirm", "rollback"
    public Button upgradeButton;   // optional: to disable/enable during request

    // Hook this in the Button's OnClick()
    public void OnUpgradeButtonClick()
    {
        Debug.Log($"Upgrade button clicked! Action: {action}");
        SendUpgradeRequest_NoCoroutine();
    }

    private void SendUpgradeRequest_NoCoroutine()
    {
        string url = "http://localhost:5263/api/playerfacilities/upgrade";

        var payload = new UpgradeRequest
        {
            TeamId = teamId,
            PlayerFacilityId = playerFacilityId,
            Action = action
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log("Sending JSON: " + json);

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        if (upgradeButton) upgradeButton.interactable = false;

        // NO coroutine. Use the async operation callback.
        var op = request.SendWebRequest();
        op.completed += _ =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Upgrade Successful:\n" + request.downloadHandler.text);

                // 🔹 Get the FacilityDetailsHandler on the same GameObject or elsewhere
                var detailsHandler = FindObjectOfType<FacilityDetailsHandler>();
                if (detailsHandler != null)
                {
                    // Make sure the IDs match before refreshing
                    detailsHandler.SetIds(teamId, playerFacilityId);

                    // 🔹 Re-fetch new level from backend and update UI
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
                Debug.LogError($"Upgrade Failed: {request.error}\n{request.downloadHandler.text}");
            }

            if (upgradeButton) upgradeButton.interactable = true;
            request.Dispose();

        };
    }

    [System.Serializable]
    private class UpgradeRequest
    {
        public string TeamId;
        public string PlayerFacilityId;
        public string Action;
    }
}
