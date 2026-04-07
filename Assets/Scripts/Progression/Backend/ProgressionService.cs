using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleJSON;


public class ProgressionService : MonoBehaviour
{
    public static ProgressionService Instance { get; private set; }

    // In-memory cache of progression states to avoid repeated file I/O
    private Dictionary<string, PlayerProgressionState> _progressionCache = 
        new Dictionary<string, PlayerProgressionState>();

    // Read-only configuration loaded from progression.json
    private ProgressionConfig _progressionConfig;

    // Events
    public event Action<string, int> OnXpUpdated; // (playerId, newTotalXp)
    public event Action<string, string> OnTierChanged; // (playerId, newTier)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgressionConfig();
    }


    private void LoadProgressionConfig()
    {
        var configPath = Path.Combine(Application.streamingAssetsPath, "Progression", "progression.json");
        
        if (!File.Exists(configPath))
        {
            Debug.LogError($"[ProgressionService] Config not found: {configPath}");
            return;
        }

        try
        {
            var json = JSONNode.Parse(File.ReadAllText(configPath));
            _progressionConfig = new ProgressionConfig(json);
            Debug.Log("[ProgressionService] Progression config loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProgressionService] Failed to parse progression config: {ex}");
        }
    }

    public PlayerProgressionState GetState(string playerId, bool createIfMissing = true)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[ProgressionService] GetState called with null/empty playerId");
            return null;
        }

        // Check cache first
        if (_progressionCache.TryGetValue(playerId, out var cachedState))
        {
            return cachedState;
        }

        // Try to load from disk
        var state = LoadProgressionStateFromDisk(playerId);
        if (state != null)
        {
            _progressionCache[playerId] = state;
            return state;
        }

        // Create new if allowed and doesn't exist
        if (createIfMissing)
        {
            state = new PlayerProgressionState(playerId);
            _progressionCache[playerId] = state;
            SaveProgressionStateToDisk(state);
            return state;
        }

        return null;
    }

   
    public void AddXp(string playerId, int xp, string source)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[ProgressionService] AddXp called with null/empty playerId");
            return;
        }

        if (xp <= 0)
        {
            Debug.LogWarning($"[ProgressionService] AddXp called with non-positive amount: {xp}");
            return;
        }

        var state = GetState(playerId, createIfMissing: true);
        if (state == null)
        {
            Debug.LogError($"[ProgressionService] Could not get or create state for {playerId}");
            return;
        }

        var historyEntry = new XpHistoryEntry(playerId, xp, source);
        state.xp_history.Add(historyEntry);

        // Update total XP
        int oldXp = state.current_xp;
        state.current_xp += xp;

        // Recalculate tier
        string oldTier = state.current_tier;
        state.current_tier = CalculateTierForXp(state.current_xp);

        // Persist changes
        SaveProgressionStateToDisk(state);

        // Publish event
        PublishXpUpdatedEvent(playerId, oldXp, state.current_xp, oldTier, state.current_tier);

        Debug.Log($"[ProgressionService] Added {xp} XP to {playerId} (source: {source}). Total: {state.current_xp}, Tier: {state.current_tier}");
    }


    public TierData GetCurrentTier(string playerId)
    {
        var state = GetState(playerId, createIfMissing: false);
        if (state == null || _progressionConfig == null)
        {
            return null;
        }

        return _progressionConfig.GetTierData(state.current_tier);
    }

    public TierData GetTierData(string tierName)
    {
        if (_progressionConfig == null)
        {
            return null;
        }

        return _progressionConfig.GetTierData(tierName);
    }

    public Dictionary<string, TierData> GetAllTiers()
    {
        return _progressionConfig?.GetAllTiers() ?? new Dictionary<string, TierData>();
    }

   
    private string CalculateTierForXp(int totalXp)
    {
        if (_progressionConfig == null)
        {
            return "rookie";
        }

        return _progressionConfig.GetTierForXp(totalXp);
    }

    private PlayerProgressionState LoadProgressionStateFromDisk(string playerId)
    {
        try
        {
            var filePath = FilePathResolver.GetProgressionPath(playerId, "progression_state.json");
            
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = JSONNode.Parse(File.ReadAllText(filePath));
            var state = new PlayerProgressionState(playerId);
            
            // Parse from JSON
            state.current_xp = json["current_xp"].AsInt;
            state.current_tier = json["current_tier"].Value;

            // Parse XP history
            state.xp_history = new List<XpHistoryEntry>();
            foreach (KeyValuePair<string, JSONNode> kvp in json["xp_history"])
            {
                var historyNode = kvp.Value;
                var entry = new XpHistoryEntry(playerId, 0, "")
                {
                    id = historyNode["id"].Value,
                    player_id = historyNode["player_id"].Value,
                    timestamp = historyNode["timestamp"].Value,
                    xp_gained = historyNode["xp_gained"].AsInt,
                    source = historyNode["source"].Value
                };
                state.xp_history.Add(entry);
            }

            return state;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProgressionService] Failed to load progression state for {playerId}: {ex}");
            return null;
        }
    }

 
    private void SaveProgressionStateToDisk(PlayerProgressionState state)
    {
        try
        {
            var filePath = FilePathResolver.GetProgressionPath(state.player_id, "progression_state.json");
            var json = new JSONObject();

            json["player_id"] = state.player_id;
            json["current_xp"] = state.current_xp;
            json["current_tier"] = state.current_tier;

            // Serialize XP history
            var historyArray = new JSONArray();
            foreach (var entry in state.xp_history)
            {
                var entryJson = new JSONObject();
                entryJson["id"] = entry.id;
                entryJson["player_id"] = entry.player_id;
                entryJson["timestamp"] = entry.timestamp;
                entryJson["xp_gained"] = entry.xp_gained;
                entryJson["source"] = entry.source;
                historyArray.Add(entryJson);
            }
            json["xp_history"] = historyArray;

            File.WriteAllText(filePath, json.ToString(2));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProgressionService] Failed to save progression state for {state.player_id}: {ex}");
        }
    }


    private void PublishXpUpdatedEvent(string playerId, int oldXp, int newXp, string oldTier, string newTier)
    {
        var payload = new JSONObject();
        payload["player_id"] = playerId;
        payload["old_xp"] = oldXp;
        payload["new_xp"] = newXp;
        payload["xp_gained"] = newXp - oldXp;
        payload["old_tier"] = oldTier;
        payload["new_tier"] = newTier;

        var evt = new EventBus.EventEnvelope
        {
            event_id = Guid.NewGuid().ToString(),
            event_type = "xp_updated",
            player_id = playerId,
            timestamp = DateTime.UtcNow.ToString("o"),
            payloadJson = payload.ToString()
        };

        EventBus.Publish(evt);

        // Fire local event
        OnXpUpdated?.Invoke(playerId, newXp);
        
        if (oldTier != newTier)
        {
            OnTierChanged?.Invoke(playerId, newTier);
        }
    }

    public void ResetAllPlayerXp()
    {
        if (_progressionCache == null || _progressionCache.Count == 0)
        {
            Debug.Log("[ProgressionService] No player progression states to reset.");
            return;
        }

        foreach (var kvp in _progressionCache)
        {
            var state = kvp.Value;
            if (state != null)
            {
                state.current_xp = 0;
                state.current_tier = "rookie"; // reset to starting tier
                state.xp_history.Clear();

                // Save the cleared state to disk
                SaveProgressionStateToDisk(state);
            }
        }

        Debug.Log("[ProgressionService] All player XP has been reset for this session.");
    }

    /// <summary>
    /// Clears all player progression data from both cache and disk
    /// Called at the start of a new session to ensure no old data persists
    /// </summary>
    public void ClearAllPlayerProgression()
    {
        try
        {
            // Clear the cache
            _progressionCache.Clear();

            // Delete all progression files from disk
            var progressionRootDir = Path.Combine(Application.streamingAssetsPath, "Progression");
            if (Directory.Exists(progressionRootDir))
            {
                // Find and delete all player progression files
                var playerDirs = Directory.GetDirectories(progressionRootDir);
                foreach (var playerDir in playerDirs)
                {
                    var progressionStateFile = Path.Combine(playerDir, "progression_state.json");
                    if (File.Exists(progressionStateFile))
                    {
                        File.Delete(progressionStateFile);
                        Debug.Log($"[ProgressionService] Deleted old progression file: {progressionStateFile}");
                    }
                }
            }

            Debug.Log("[ProgressionService] All player progression data cleared for new session.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProgressionService] Failed to clear player progression data: {ex}");
        }
    }

   

    private class ProgressionConfig
    {
        private Dictionary<string, TierData> _tiers = new Dictionary<string, TierData>();

        public ProgressionConfig(JSONNode configJson)
        {
            // Parse tier_progression section
            var tiersNode = configJson["tier_progression"];
            foreach (KeyValuePair<string, JSONNode> kvp in tiersNode)
            {
                var tierName = kvp.Key;
                var tierNode = kvp.Value;
                var tierData = new TierData
                {
                    min_xp = tierNode["min_xp"].AsInt,
                    max_xp = tierNode["max_xp"].AsInt,
                    display_name = tierNode["display_name"].Value,
                    unlock_features = new List<string>()
                };

                foreach (var feature in tierNode["unlock_features"].AsArray)
                {
                    tierData.unlock_features.Add(feature.Value);
                }

                _tiers[tierName] = tierData;
            }
        }

        public string GetTierForXp(int totalXp)
        {
            // Find the appropriate tier based on XP
            string currentTier = "rookie";
            foreach (var kvp in _tiers)
            {
                if (totalXp >= kvp.Value.min_xp && totalXp <= kvp.Value.max_xp)
                {
                    return kvp.Key;
                }
            }
            return currentTier;
        }

        public TierData GetTierData(string tierName)
        {
            _tiers.TryGetValue(tierName, out var tierData);
            return tierData;
        }

        public Dictionary<string, TierData> GetAllTiers()
        {
            return _tiers;
        }
       
    }

    public Dictionary<string, PlayerProgressionState> GetAllStates()
    {
        // Return a shallow copy to avoid external modification
        return new Dictionary<string, PlayerProgressionState>(_progressionCache);
    }
}
