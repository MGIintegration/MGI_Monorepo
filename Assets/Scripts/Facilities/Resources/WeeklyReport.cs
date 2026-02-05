using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class WeeklySnapshotResponseDTO {
    public string TeamId;
    public List<WeeklyFacilityItemDTO> Facilities;
}
[System.Serializable]
public class WeeklyFacilityItemDTO {
    public string PlayerFacilityId;
    public string FacilityName;
    public int Level;
    public string WeeklyBoostText;
}

public class WeeklyReport : MonoBehaviour
{
    [Header("API")]
    public string apiBaseUrl = "http://localhost:5263/api";
    public string endpointPath = "/weeklysnapshot";
    [Tooltip("Must match the TeamId in your DB")]
    public string teamId = "d4e5f6a7-b8c9-0123-4567-890abcdef012";

    [Header("UI")]
    public TMP_Text outputText;   // one text field

    [Header("Behavior")]
    public bool autoRefreshOnEnable = true;

    void OnEnable()
    {
        
        if (autoRefreshOnEnable)
            StartCoroutine(FetchAndBind());
    }

    IEnumerator FetchAndBind()
    {
        if (!outputText) yield break;

        string url = $"{apiBaseUrl.TrimEnd('/')}/{endpointPath.TrimStart('/')}?teamId={UnityWebRequest.EscapeURL(teamId)}";
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool isError = req.result != UnityWebRequest.Result.Success;
#else
            bool isError = req.isHttpError || req.isNetworkError;
#endif
            if (isError)
            {
                outputText.text = $"Error {req.responseCode}: {req.error}";
                yield break;
            }

            WeeklySnapshotResponseDTO dto;
            try { dto = JsonConvert.DeserializeObject<WeeklySnapshotResponseDTO>(req.downloadHandler.text); }
            catch (System.Exception ex) { outputText.text = "Parse error: " + ex.Message; yield break; }

            if (dto == null || dto.Facilities == null || dto.Facilities.Count == 0)
            {
                outputText.text = "No facilities found for this team.";
                yield break;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var f in dto.Facilities)
            {
                sb.AppendLine($"{f.FacilityName} (Lv {f.Level})");
                sb.AppendLine($"- {(!string.IsNullOrWhiteSpace(f.WeeklyBoostText) ? f.WeeklyBoostText : "-")}");
                sb.AppendLine();
            }
            outputText.text = sb.ToString().TrimEnd();
        }
    }
}
