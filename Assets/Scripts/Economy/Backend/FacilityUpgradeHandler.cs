using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class FacilityUpgradeHandler : MonoBehaviour
{
    public Button upgradeButton;

    [Header("Set these in Inspector")]
    public string teamId;
    public string playerFacilityId;
    public string action; // "start", "confirm", "rollback"

    private void OnEnable()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
    }

    private void OnDisable()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClick);
    }

    private void OnUpgradeButtonClick()
    {
        Debug.Log($"Upgrade button clicked! Action: {action}");
        StartCoroutine(SendUpgradeRequest());
    }

    private IEnumerator SendUpgradeRequest()
    {
        if (!this.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot start coroutine — object is inactive.");
            yield break;
        }

        string url = "http://localhost:5263/api/playerfacilities/upgrade";

        UpgradeRequest requestData = new UpgradeRequest
        {
            TeamId = teamId,
            PlayerFacilityId = playerFacilityId,
            Action = action
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        Debug.Log("Sending JSON: " + jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Upgrade Successful:\n" + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"Upgrade Failed: {request.error}\n{request.downloadHandler.text}");
        }
    }

    [System.Serializable]
    public class UpgradeRequest
    {
        public string TeamId;
        public string PlayerFacilityId;
        public string Action;
    }
}
