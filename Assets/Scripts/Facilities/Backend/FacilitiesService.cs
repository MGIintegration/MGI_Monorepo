using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class FacilitiesService
{
    public const string DefaultPlayerId = "local_player";
    private const string FacilitiesConfigFileName = "facilities_config.json";
    private const string PlayerFacilitiesFileName = "player_facilities.json";
    private const string UpgradeFacilitySpendSource = "upgrade_facility";

    private readonly EconomyService _economy = new EconomyService();
    private FacilitiesConfigRoot _configCache;

    private readonly HashSet<string> _validFacilityTypeIds = new()
    {
        "weight_room",
        "rehab_center",
        "film_room"
    };

    /// <summary>
    /// Upgrades a facility by one level for the given player.
    /// Deducts the upgrade cost via EconomyService.TrySpend (source: "upgrade_facility");
    /// returns false without changing state if funds are insufficient.
    /// On success, increments the level, saves player_facilities.json, and publishes
    /// an "upgrade_facility" event on the shared EventBus.
    /// Returns the updated PlayerFacilityProgress in newState.
    /// </summary>
    public bool TryUpgradeFacility(string playerId, string facilityTypeId, out PlayerFacilityProgress newState)
    {
        newState = null;

        if (string.IsNullOrWhiteSpace(playerId))
        {
            Debug.LogError("FacilitiesService.TryUpgradeFacility: playerId is required.");
            return false;
        }

        if (!IsValidFacilityType(facilityTypeId))
        {
            Debug.LogError($"FacilitiesService.TryUpgradeFacility: invalid facilityTypeId '{facilityTypeId}'.");
            return false;
        }

        var config = LoadFacilityConfig(facilityTypeId);
        if (config == null || config.levels == null || config.levels.Count == 0)
        {
            Debug.LogError($"FacilitiesService.TryUpgradeFacility: failed to load config for '{facilityTypeId}'.");
            return false;
        }

        var playerState = GetPlayerFacilityState(playerId);
        var progress = GetOrCreateFacilityProgress(playerState, facilityTypeId);

        int maxLevel = config.max_level > 0 ? config.max_level : config.levels.Max(l => l.level);
        if (progress.level >= maxLevel)
        {
            Debug.LogWarning($"FacilitiesService.TryUpgradeFacility: '{facilityTypeId}' already at max level.");
            newState = progress;
            return false;
        }

        int nextLevel = progress.level + 1;
        var nextLevelConfig = config.levels.FirstOrDefault(l => l.level == nextLevel);
        if (nextLevelConfig == null)
        {
            Debug.LogError($"FacilitiesService.TryUpgradeFacility: no config found for '{facilityTypeId}' level {nextLevel}.");
            return false;
        }

        int costCoins = nextLevelConfig.upgrade_cost;
        int costGems = 0;

        if (!_economy.TrySpend(playerId, costCoins, costGems, UpgradeFacilitySpendSource))
        {
            Debug.LogWarning(
                $"FacilitiesService.TryUpgradeFacility: insufficient funds for '{facilityTypeId}' " +
                $"(need {costCoins} coins, {costGems} gems).");
            newState = progress;
            return false;
        }

        progress.level = nextLevel;
        SavePlayerFacilityState(playerState);

        PublishUpgradeFacilityEvent(playerId, facilityTypeId, nextLevel, costCoins, costGems);

        newState = progress;
        return true;
    }

    private void PublishUpgradeFacilityEvent(
        string playerId,
        string facilityTypeId,
        int newLevel,
        int costCoins,
        int costGems)
    {
        var payload = new
        {
            facility_type_id = facilityTypeId,
            new_level = newLevel,
            cost_coins = costCoins,
            cost_gems = costGems
        };

        try
        {
            EventBus.Publish(new EventBus.EventEnvelope
            {
                event_type = UpgradeFacilitySpendSource,
                player_id = playerId,
                payloadJson = JsonConvert.SerializeObject(payload)
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                $"FacilitiesService.PublishUpgradeFacilityEvent: failed to publish event. {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the full PlayerFacilityState for Progression or any other read-only consumer.
    /// Creates a default file if none exists yet.
    /// </summary>
    public PlayerFacilityState GetPlayerFacilityState(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            playerId = DefaultPlayerId;
        }

        var root = LoadPlayerFacilitiesRoot(playerId);
        var state = root.player_facilities.FirstOrDefault(p => p.player_id == playerId);

        bool shouldSave = false;

        if (state == null)
        {
            state = CreateDefaultPlayerFacilityState(playerId);
            root.player_facilities.Add(state);
            shouldSave = true;
        }

        state.facilities ??= new Dictionary<string, PlayerFacilityProgress>();
        shouldSave |= EnsureKnownFacilityProgress(state);

        if (shouldSave)
        {
            SavePlayerFacilitiesRoot(playerId, root);
        }

        return state;
    }

    /// <summary>
    /// Returns a single facility progress record for the requested facility type.
    /// If the player has no saved state for that facility yet, a default level-1 progress record is returned/created.
    /// </summary>
    public PlayerFacilityProgress GetFacilityProgress(string playerId, string facilityTypeId)
    {
        if (!IsValidFacilityType(facilityTypeId))
        {
            Debug.LogWarning($"FacilitiesService.GetFacilityProgress: invalid facilityTypeId '{facilityTypeId}'.");
            return null;
        }

        var state = GetPlayerFacilityState(playerId);
        return GetOrCreateFacilityProgress(state, facilityTypeId);
    }

    /// <summary>
    /// Returns the active effects for the player's current level of the given facility.
    /// This is useful for Progression when applying XP multipliers.
    /// </summary>
    public Dictionary<string, float> GetFacilityEffects(string playerId, string facilityTypeId)
    {
        if (!IsValidFacilityType(facilityTypeId))
        {
            Debug.LogWarning($"FacilitiesService.GetFacilityEffects: invalid facilityTypeId '{facilityTypeId}'.");
            return new Dictionary<string, float>();
        }

        var config = LoadFacilityConfig(facilityTypeId);
        if (config == null || config.levels == null || config.levels.Count == 0)
        {
            return new Dictionary<string, float>();
        }

        var progress = GetFacilityProgress(playerId, facilityTypeId);
        if (progress == null)
        {
            return new Dictionary<string, float>();
        }

        var levelData = config.levels.FirstOrDefault(l => l.level == progress.level);
        return levelData?.benefits ?? new Dictionary<string, float>();
    }

    /// <summary>
    /// Returns all current facility effects keyed by facility_type_id.
    /// This is convenient if Progression wants one call instead of three separate calls.
    /// </summary>
    public Dictionary<string, Dictionary<string, float>> GetAllFacilityEffects(string playerId)
    {
        var result = new Dictionary<string, Dictionary<string, float>>();

        foreach (var facilityTypeId in _validFacilityTypeIds)
        {
            result[facilityTypeId] = GetFacilityEffects(playerId, facilityTypeId);
        }

        return result;
    }

    /// <summary>
    /// Returns a typed read-only snapshot for Progression or any other consumer
    /// that needs facility levels, effects, and precomputed XP multipliers.
    /// </summary>
    public ProgressionFacilitySnapshot GetProgressionSnapshot(string playerId)
    {
        var normalizedPlayerId = string.IsNullOrWhiteSpace(playerId) ? DefaultPlayerId : playerId;
        var levels = new Dictionary<string, int>();
        var effects = new Dictionary<string, Dictionary<string, float>>();

        foreach (var facilityTypeId in _validFacilityTypeIds)
        {
            var progress = GetFacilityProgress(normalizedPlayerId, facilityTypeId);
            levels[facilityTypeId] = progress?.level ?? 1;
            effects[facilityTypeId] = GetFacilityEffects(normalizedPlayerId, facilityTypeId);
        }

        return new ProgressionFacilitySnapshot
        {
            player_id = normalizedPlayerId,
            facility_levels = levels,
            facility_effects = effects,
            match_xp_multiplier = CalculateMatchXpMultiplier(effects),
            training_xp_multiplier = CalculateTrainingXpMultiplier(effects),
            recovery_xp_multiplier = CalculateRecoveryXpMultiplier(effects)
        };
    }

    /// <summary>
    /// Returns the Facilities-defined multiplier that Progression should apply
    /// for a given XP source.
    /// Supported source values include match, training, and recovery.
    /// </summary>
    public float GetProgressionXpMultiplier(string playerId, string source)
    {
        var snapshot = GetProgressionSnapshot(playerId);
        string normalizedSource = string.IsNullOrWhiteSpace(source) ? "match" : source.Trim().ToLowerInvariant();

        return normalizedSource switch
        {
            "match" or "match_win" or "match_loss" => snapshot.match_xp_multiplier,
            "training" or "training_session" or "practice" => snapshot.training_xp_multiplier,
            "recovery" or "rehab" => snapshot.recovery_xp_multiplier,
            _ => snapshot.match_xp_multiplier
        };
    }

    public bool IsValidFacilityType(string facilityTypeId)
    {
        return !string.IsNullOrWhiteSpace(facilityTypeId)
            && _validFacilityTypeIds.Contains(facilityTypeId);
    }

    private FacilityDefinition LoadFacilityConfig(string facilityTypeId)
    {
        if (!IsValidFacilityType(facilityTypeId))
        {
            return null;
        }

        var root = LoadFacilitiesConfigRoot();
        return root?.facility_definitions?
            .FirstOrDefault(f => f.facility_type_id == facilityTypeId);
    }

    private FacilitiesConfigRoot LoadFacilitiesConfigRoot()
    {
        if (_configCache != null)
        {
            return _configCache;
        }

        string path = Path.Combine(
            Application.streamingAssetsPath,
            "Facilities",
            FacilitiesConfigFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"FacilitiesService.LoadFacilitiesConfigRoot: config not found at {path}.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            _configCache = JsonConvert.DeserializeObject<FacilitiesConfigRoot>(json);

            if (_configCache == null)
            {
                Debug.LogError("FacilitiesService.LoadFacilitiesConfigRoot: facilities_config.json is empty or invalid.");
                return null;
            }

            _configCache.facility_definitions ??= new List<FacilityDefinition>();
            return _configCache;
        }
        catch (Exception ex)
        {
            Debug.LogError($"FacilitiesService.LoadFacilitiesConfigRoot: failed to parse config. {ex.Message}");
            return null;
        }
    }

    private PlayerFacilitiesRoot LoadPlayerFacilitiesRoot(string playerId)
    {
        string path = FilePathResolver.GetFacilitiesPath(playerId, PlayerFacilitiesFileName);

        if (!File.Exists(path))
        {
            return new PlayerFacilitiesRoot();
        }

        try
        {
            string json = File.ReadAllText(path);
            var root = JsonConvert.DeserializeObject<PlayerFacilitiesRoot>(json);

            if (root?.player_facilities != null)
            {
                return root;
            }

            var legacyState = JsonConvert.DeserializeObject<LegacyPlayerFacilityState>(json);
            if (legacyState != null && !string.IsNullOrWhiteSpace(legacyState.player_id))
            {
                return new PlayerFacilitiesRoot
                {
                    player_facilities = new List<PlayerFacilityState>
                    {
                        ConvertLegacyState(legacyState)
                    }
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"FacilitiesService.LoadPlayerFacilitiesRoot: failed to read state. {ex.Message}");
        }

        return new PlayerFacilitiesRoot();
    }

    private void SavePlayerFacilityState(PlayerFacilityState state)
    {
        if (state == null || string.IsNullOrWhiteSpace(state.player_id))
        {
            Debug.LogError("FacilitiesService.SavePlayerFacilityState: invalid state.");
            return;
        }

        state.facilities ??= new Dictionary<string, PlayerFacilityProgress>();

        var root = LoadPlayerFacilitiesRoot(state.player_id);
        root.player_facilities.RemoveAll(p => p.player_id == state.player_id);
        root.player_facilities.Add(state);

        SavePlayerFacilitiesRoot(state.player_id, root);
    }

    private void SavePlayerFacilitiesRoot(string playerId, PlayerFacilitiesRoot root)
    {
        root ??= new PlayerFacilitiesRoot();
        root.player_facilities ??= new List<PlayerFacilityState>();

        string path = FilePathResolver.GetFacilitiesPath(playerId, PlayerFacilitiesFileName);

        try
        {
            string json = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"FacilitiesService.SavePlayerFacilitiesRoot: failed to save state. {ex.Message}");
        }
    }

    private PlayerFacilityState CreateDefaultPlayerFacilityState(string playerId)
    {
        return new PlayerFacilityState
        {
            player_id = playerId,
            facilities = new Dictionary<string, PlayerFacilityProgress>()
        };
    }

    private PlayerFacilityProgress GetOrCreateFacilityProgress(PlayerFacilityState state, string facilityTypeId)
    {
        state.facilities ??= new Dictionary<string, PlayerFacilityProgress>();

        if (!state.facilities.TryGetValue(facilityTypeId, out var progress) || progress == null)
        {
            progress = new PlayerFacilityProgress
            {
                level = 1
            };

            state.facilities[facilityTypeId] = progress;
        }

        return progress;
    }

    private bool EnsureKnownFacilityProgress(PlayerFacilityState state)
    {
        bool changed = false;

        foreach (var facilityTypeId in _validFacilityTypeIds)
        {
            if (state.facilities.TryGetValue(facilityTypeId, out var progress) && progress != null)
            {
                if (progress.level < 1)
                {
                    progress.level = 1;
                    changed = true;
                }

                continue;
            }

            state.facilities[facilityTypeId] = new PlayerFacilityProgress
            {
                level = 1
            };
            changed = true;
        }

        return changed;
    }

    private PlayerFacilityState ConvertLegacyState(LegacyPlayerFacilityState legacyState)
    {
        var state = CreateDefaultPlayerFacilityState(legacyState.player_id);

        if (legacyState.facilities == null)
        {
            return state;
        }

        foreach (var kvp in legacyState.facilities)
        {
            if (!IsValidFacilityType(kvp.Key) || kvp.Value == null)
            {
                continue;
            }

            state.facilities[kvp.Key] = new PlayerFacilityProgress
            {
                level = Mathf.Max(1, kvp.Value.level)
            };
        }

        return state;
    }

    private float CalculateMatchXpMultiplier(Dictionary<string, Dictionary<string, float>> allEffects)
    {
        float bonus =
            GetEffectValue(allEffects, "film_room", "PlayerIntelligenceBoost") +
            GetEffectValue(allEffects, "film_room", "GamePlanEffectiveness");

        return 1f + bonus;
    }

    private float CalculateTrainingXpMultiplier(Dictionary<string, Dictionary<string, float>> allEffects)
    {
        float bonus =
            GetEffectValue(allEffects, "weight_room", "PlayerStrengthBoost") +
            GetEffectValue(allEffects, "weight_room", "PlayerConditioningBoost") +
            GetEffectValue(allEffects, "weight_room", "FatigueResistance");

        return 1f + bonus;
    }

    private float CalculateRecoveryXpMultiplier(Dictionary<string, Dictionary<string, float>> allEffects)
    {
        float bonus =
            GetEffectValue(allEffects, "rehab_center", "InjuryRecoveryMultiplier") +
            GetEffectValue(allEffects, "rehab_center", "PlayerHealthBoost") +
            GetEffectValue(allEffects, "rehab_center", "InjuryRiskReduction");

        return 1f + bonus;
    }

    private float GetEffectValue(
        Dictionary<string, Dictionary<string, float>> allEffects,
        string facilityTypeId,
        string effectKey)
    {
        if (!allEffects.TryGetValue(facilityTypeId, out var effects) || effects == null)
        {
            return 0f;
        }

        if (!effects.TryGetValue(effectKey, out var value))
        {
            return 0f;
        }

        if (effectKey.EndsWith("Multiplier"))
        {
            return Mathf.Max(0f, value - 1f);
        }

        return Mathf.Max(0f, value);
    }
}

[System.Serializable]
public class FacilitiesConfigRoot
{
    public string schema_version = "1.0";
    public List<FacilityDefinition> facility_definitions;
}

[System.Serializable]
public class FacilityDefinition
{
    public string facility_type_id;
    public string display_name;
    public int max_level;
    public List<FacilityLevel> levels;
}

[System.Serializable]
public class FacilityLevel
{
    public int level;
    public int upgrade_cost;
    public string descriptor;
    public Dictionary<string, float> benefits;
    public Validation validation;
}

[System.Serializable]
public class Validation
{
    public int minCost;
    public int maxCost;
    public Dictionary<string, float> effectCaps;
}

[System.Serializable]
public class PlayerFacilitiesRoot
{
    public string schema_version = "1.0";
    public List<PlayerFacilityState> player_facilities = new();
}

[System.Serializable]
public class PlayerFacilityState
{
    public string player_id;
    public Dictionary<string, PlayerFacilityProgress> facilities;
}

[System.Serializable]
public class PlayerFacilityProgress
{
    public int level;
}

[System.Serializable]
public class ProgressionFacilitySnapshot
{
    public string player_id;
    public Dictionary<string, int> facility_levels;
    public Dictionary<string, Dictionary<string, float>> facility_effects;
    public float match_xp_multiplier;
    public float training_xp_multiplier;
    public float recovery_xp_multiplier;
}

[System.Serializable]
internal class LegacyPlayerFacilityState
{
    public string player_id;
    public Dictionary<string, LegacyPlayerFacilityProgress> facilities;
}

[System.Serializable]
internal class LegacyPlayerFacilityProgress
{
    public string facility_type_id;
    public int level;
}
