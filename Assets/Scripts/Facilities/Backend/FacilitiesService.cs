using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class FacilitiesService
{
    private const string DefaultFacilityResourceName = "WeightRoom";
    private const string LocalStateFileName = "facilityStatus.json";

    public FacilityUpgradeResult TryUpgradeFacility(string teamId, string playerFacilityId, string action)
    {
        // ---------------- INPUT VALIDATION ----------------
        if (string.IsNullOrWhiteSpace(teamId))
            return Fail("TeamId is required.");

        if (string.IsNullOrWhiteSpace(playerFacilityId))
            return Fail("PlayerFacilityId is required.");

        if (string.IsNullOrWhiteSpace(action))
            return Fail("Action is required.");

        if (action != "start" && action != "confirm" && action != "rollback")
            return Fail($"Unsupported action: {action}");

        // For now, only "confirm" will actually perform an upgrade.
        // "start" and "rollback" are accepted but not yet implemented with separate behavior.
        if (action != "confirm")
        {
            return new FacilityUpgradeResult
            {
                Success = true,
                Message = $"Action '{action}' accepted, but no local state change was applied."
            };
        }

        // ---------------- LOAD FACILITY CONFIG ----------------
        TextAsset configJson = Resources.Load<TextAsset>(DefaultFacilityResourceName);
        if (configJson == null)
            return Fail($"Facility config '{DefaultFacilityResourceName}.json' not found in Resources.");

        FacilityConfigRoot config;
        try
        {
            config = JsonConvert.DeserializeObject<FacilityConfigRoot>(configJson.text);
        }
        catch (System.Exception ex)
        {
            return Fail($"Failed to parse facility config JSON. {ex.Message}");
        }

        if (config == null || config.levels == null || config.levels.Count == 0)
            return Fail("Invalid facility config: no levels found.");

        // ---------------- LOAD OR CREATE LOCAL PLAYER STATE ----------------
        string path = Path.Combine(Application.persistentDataPath, LocalStateFileName);
        FacilityState state = null;

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                state = JsonConvert.DeserializeObject<FacilityState>(json);
            }
            catch (System.Exception ex)
            {
                return Fail($"Failed to read local facility state. {ex.Message}");
            }
        }

        if (state == null)
        {
            state = new FacilityState
            {
                TeamId = teamId,
                PlayerFacilityId = playerFacilityId,
                CurrentLevel = 1
            };
        }

        // If the saved state belongs to another team/facility, reset it for the current request.
        if (state.TeamId != teamId || state.PlayerFacilityId != playerFacilityId)
        {
            state.TeamId = teamId;
            state.PlayerFacilityId = playerFacilityId;
            state.CurrentLevel = Mathf.Max(1, state.CurrentLevel);
        }

        // ---------------- UPGRADE LOGIC ----------------
        int maxLevel = config.levels.Max(l => l.level);

        if (state.CurrentLevel >= maxLevel)
            return Fail("Already at max level.");

        int nextLevel = state.CurrentLevel + 1;
        FacilityLevel nextLevelData = config.levels.FirstOrDefault(l => l.level == nextLevel);

        if (nextLevelData == null)
            return Fail($"Next level configuration for level {nextLevel} was not found.");

        // Add EconomyService.TrySpend(...) here before applying the upgrade.

        state.CurrentLevel = nextLevel;

        // ---------------- SAVE UPDATED STATE ----------------
        try
        {
            string updatedJson = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(path, updatedJson);
        }
        catch (System.Exception ex)
        {
            return Fail($"Failed to save updated facility state. {ex.Message}");
        }

        return new FacilityUpgradeResult
        {
            Success = true,
            Message = $"Facility upgraded successfully to level {nextLevel}."
        };
    }

    private FacilityUpgradeResult Fail(string message)
    {
        return new FacilityUpgradeResult
        {
            Success = false,
            Message = message
        };
    }

    [System.Serializable]
    public class FacilityState
    {
        public string TeamId;
        public string PlayerFacilityId;
        public int CurrentLevel;
    }

    [System.Serializable]
    public class FacilityConfigRoot
    {
        public List<FacilityLevel> levels;
    }

    [System.Serializable]
    public class FacilityLevel
    {
        public int level;
        public int upgradeCost;
    }
}