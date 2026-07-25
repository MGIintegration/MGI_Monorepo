using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WeeklyReport : MonoBehaviour
{
    static readonly (string facilityTypeId, string displayName)[] FacilityDisplayOrder =
    {
        ("weight_room", "Weight Room"),
        ("rehab_center", "Rehab Center"),
        ("film_room", "Film Room"),
    };

    [Header("IDs (local mode)")]
    public string teamId = FacilitiesService.DefaultPlayerId; // treated as playerId in local mode

    [Header("UI")]
    public TMP_Text outputText;   // one text field

    [Header("Behavior")]
    public bool autoRefreshOnEnable = true;

    readonly FacilitiesService _facilitiesService = new();

    void OnEnable()
    {
        if (autoRefreshOnEnable)
            BindLocalSnapshot();
    }

    public void BindLocalSnapshot()
    {
        if (!outputText) return;

        string playerId = string.IsNullOrWhiteSpace(teamId) ? FacilitiesService.DefaultPlayerId : teamId;
        var snapshot = _facilitiesService.GetProgressionSnapshot(playerId);

        var sb = new System.Text.StringBuilder();
        foreach (var (facilityTypeId, displayName) in FacilityDisplayOrder)
        {
            int level = snapshot.facility_levels.TryGetValue(facilityTypeId, out var lvl) ? lvl : 1;
            var effects = snapshot.facility_effects.TryGetValue(facilityTypeId, out var eff)
                ? eff
                : new Dictionary<string, float>();

            sb.AppendLine($"{displayName} (Lv {level})");
            sb.AppendLine($"- {FacilityDetailsHandler.FormatWeeklyBoost(effects)}");
            sb.AppendLine();
        }
        outputText.text = sb.ToString().TrimEnd();
    }
}
