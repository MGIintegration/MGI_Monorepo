using System.IO;
using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;

public class ProgressionSummaryPanel : MonoBehaviour
{
    [Header("IDs (local mode)")]
    public string playerId = FacilitiesService.DefaultPlayerId;

    [Header("UI")]
    public TMP_Text outputText;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!outputText) return;

        string id = string.IsNullOrWhiteSpace(playerId) ? FacilitiesService.DefaultPlayerId : playerId;

        int currentXp = 0;
        string currentTier = "rookie";

        var statePath = FilePathResolver.GetProgressionPath(id, "progression_state.json");
        if (File.Exists(statePath))
        {
            var state = JObject.Parse(File.ReadAllText(statePath));
            currentXp = state.Value<int?>("current_xp") ?? 0;
            currentTier = state.Value<string>("current_tier") ?? "rookie";
        }

        string tierDisplayName = currentTier;
        int tierMaxXp = 0;
        string nextTierDisplayName = null;
        int nextTierMinXp = 0;

        var configPath = Path.Combine(Application.streamingAssetsPath, "Progression", "progression.json");
        if (File.Exists(configPath))
        {
            var tiers = JObject.Parse(File.ReadAllText(configPath))["tier_progression"] as JObject;
            if (tiers != null && tiers.TryGetValue(currentTier, out var tierToken))
            {
                tierDisplayName = tierToken.Value<string>("display_name") ?? currentTier;
                tierMaxXp = tierToken.Value<int?>("max_xp") ?? 0;
            }

            if (tiers != null)
            {
                foreach (var prop in tiers.Properties())
                {
                    int min = prop.Value.Value<int?>("min_xp") ?? 0;
                    if (min == tierMaxXp + 1)
                    {
                        nextTierDisplayName = prop.Value.Value<string>("display_name");
                        nextTierMinXp = min;
                        break;
                    }
                }
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Current Tier: {tierDisplayName}");
        sb.AppendLine($"Total XP: {currentXp}");
        sb.AppendLine();
        sb.AppendLine(nextTierDisplayName != null
            ? $"Next Tier: {nextTierDisplayName} ({Mathf.Max(0, nextTierMinXp - currentXp)} XP to go)"
            : "Highest tier reached.");

        outputText.text = sb.ToString().TrimEnd();
    }
}
