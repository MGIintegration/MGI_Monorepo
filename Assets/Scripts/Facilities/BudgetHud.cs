using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Globalization;

public class BudgetHud : MonoBehaviour
{
    [Header("UI refs (assign in Inspector)")]
    public TMP_Text budgetText;            // drag the "BudgetText" object here
    public TMP_Text recoveryBoostText;     // drag the "RecoveryBoost" object here (optional)

    [Header("Server")]
    public string apiBaseUrl = "http://localhost:5263/api"; // e.g. http://localhost:5263/api
    public string budgetPath = "/teams/budget";             // your controller is TeamsController

    [Header("IDs")]
    public string teamId; // paste your team_id from DB exactly

    void Awake()
    {
        // sanity: warn if unassigned
        if (!budgetText) Debug.LogError("[BudgetHud] 'budgetText' is not assigned in Inspector.");
        if (string.IsNullOrWhiteSpace(apiBaseUrl)) Debug.LogError("[BudgetHud] 'apiBaseUrl' is empty.");
        if (string.IsNullOrWhiteSpace(budgetPath)) Debug.LogError("[BudgetHud] 'budgetPath' is empty.");
        if (string.IsNullOrWhiteSpace(teamId)) Debug.LogError("[BudgetHud] 'teamId' is empty.");
        Debug.Log("DEVICE_ID = " + DeviceIdProvider.GetOrCreateDeviceId());
    }

    void OnEnable() => RefreshFromServer();  // run even if object was disabled at load
    void Start() => RefreshFromServer();  // double-trigger to be safe

    public void RefreshFromServer()
    {
        StartCoroutine(FetchAndBind());
    }

    System.Collections.IEnumerator FetchAndBind()
    {
        string url = $"{apiBaseUrl.TrimEnd('/')}/{budgetPath.TrimStart('/')}?teamId={teamId}";
        Debug.Log("[BudgetHud] GET " + url);

        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool isError = req.result != UnityWebRequest.Result.Success;
#else
            bool isError = req.isNetworkError || req.isHttpError;
#endif
            if (isError)
            {
                Debug.LogError($"[BudgetHud] Request failed ({req.responseCode}): {req.error}\nBody: {req.downloadHandler.text}");
                if (budgetText) budgetText.text = "FACILITY BUDGET: [no data]";
                yield break;
            }

            var body = req.downloadHandler.text;
            Debug.Log("[BudgetHud] OK: " + body);

            BudgetDto dto = null;
            try { dto = JsonConvert.DeserializeObject<BudgetDto>(body); }
            catch (System.Exception ex)
            {
                Debug.LogError("[BudgetHud] JSON parse error: " + ex.Message + "\nBody: " + body);
                if (budgetText) budgetText.text = "FACILITY BUDGET: [parse error]";
                yield break;
            }

            if (dto == null)
            {
                if (budgetText) budgetText.text = "FACILITY BUDGET: [null]";
                yield break;
            }

            if (budgetText)
            {
                // format like $80,000 (no decimals)
                var usd = dto.Budget.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
                budgetText.text = $"FACILITY BUDGET: {usd}";
            }
            if (recoveryBoostText)
            {
                recoveryBoostText.text = $"RECOVERY BOOST: +{dto.RecoveryBoostPercent:0.#}%";
            }
        }
    }

    class BudgetDto
    {
        public string TeamId;
        public decimal Budget;           // matches controller anonymous object
        public float RecoveryBoostPercent;
    }
}
