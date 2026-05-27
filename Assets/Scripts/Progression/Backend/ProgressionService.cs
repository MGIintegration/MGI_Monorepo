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

   
    /// <summary>
    /// Records an XP grant. Callers pass the final amount to store (integration plan §2.2).
    /// eventId prevents applying the same grant twice (e.g. CCAS pack opens).
    /// </summary>
    public void AddXp(string playerId, int xp, string source, string eventId)
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

        // Idempotent grant: same eventId must not apply XP twice (e.g. CCAS pack re-open).
        bool isDuplicateEvent = state.xp_history.Exists(e => e.id == eventId);
        if (isDuplicateEvent)
        {
            Debug.Log($"[ProgressionService] Duplicate XP ignored: {eventId}");
            return;
        }

        var historyEntry = new XpHistoryEntry(playerId, xp, source)
        {
            id = eventId
        };

        state.xp_history.Add(historyEntry);

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
            var statePath = FilePathResolver.GetProgressionPath(playerId, "progression_state.json");
            var historyPath = FilePathResolver.GetProgressionPath(playerId, "xp_history.jsonl");
            
            var state = new PlayerProgressionState(playerId);
            
            // Load current state (XP and tier) if it exists
            // If progression_state.json doesn't exist, state starts with defaults (0 XP, rookie tier)
            if (File.Exists(statePath))
            {
                var json = JSONNode.Parse(File.ReadAllText(statePath));
                state.current_xp = json["current_xp"].AsInt;
                state.current_tier = json["current_tier"].Value;
            }

            // Load XP history for this player only (jsonl may contain legacy mixed entries)
            state.xp_history = LoadXpHistoryForPlayer(historyPath, playerId);

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
            var statePath = FilePathResolver.GetProgressionPath(state.player_id, "progression_state.json");
            var historyPath = FilePathResolver.GetProgressionPath(state.player_id, "xp_history.jsonl");
            
            // Save current state (XP and tier) - overwrite
            var json = new JSONObject();
            json["player_id"] = state.player_id;
            json["current_xp"] = state.current_xp;
            json["current_tier"] = state.current_tier;
            File.WriteAllText(statePath, json.ToString(2));

            // Append NEW XP history entries to JSONL (one entry per line)
            // Load existing history count to know which entries to append
            int existingEntryCount = 0;
            if (File.Exists(historyPath))
            {
                var existingLines = File.ReadAllLines(historyPath);
                existingEntryCount = existingLines.Length;
            }

            // Append only new entries
            if (state.xp_history.Count > existingEntryCount)
            {
                using (var writer = File.AppendText(historyPath))
                {
                    for (int i = existingEntryCount; i < state.xp_history.Count; i++)
                    {
                        var entry = state.xp_history[i];
                        var entryJson = new JSONObject();
                        entryJson["id"] = entry.id;
                        entryJson["player_id"] = entry.player_id;
                        entryJson["timestamp"] = entry.timestamp;
                        entryJson["xp_gained"] = entry.xp_gained;
                        entryJson["source"] = entry.source;
                        writer.WriteLine(entryJson.ToString());
                    }
                }
            }
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
    /// Clears progression state and XP history for one player (new season/session).
    /// </summary>
    public void ClearPlayerProgression(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[ProgressionService] ClearPlayerProgression called with null/empty playerId");
            return;
        }

        _progressionCache.Remove(playerId);

        try
        {
            var progressionDir = Path.GetDirectoryName(
                FilePathResolver.GetProgressionPath(playerId, "progression_state.json"));
            if (!Directory.Exists(progressionDir)) return;

            var stateFile = Path.Combine(progressionDir, "progression_state.json");
            var historyFile = Path.Combine(progressionDir, "xp_history.jsonl");
            if (File.Exists(stateFile)) File.Delete(stateFile);
            if (File.Exists(historyFile)) File.Delete(historyFile);

            Debug.Log($"[ProgressionService] Cleared progression data for {playerId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProgressionService] Failed to clear progression for {playerId}: {ex}");
        }
    }

    /// <summary>
    /// Clears all player progression data from both cache and disk.
    /// Called at app/session start before a new season is created.
    /// </summary>
    public void ClearAllPlayerProgression()
    {
        try
        {
            // Clear the cache
            _progressionCache.Clear();

            // Delete all progression files from disk (persistentDataPath, not StreamingAssets)
            var progressionRootDir = Path.Combine(Application.persistentDataPath, "mgi_state");
            if (Directory.Exists(progressionRootDir))
            {
                // Find all player directories
                var playerDirs = Directory.GetDirectories(progressionRootDir);
                foreach (var playerDir in playerDirs)
                {
                    var progressionDir = Path.Combine(playerDir, "progression");
                    if (Directory.Exists(progressionDir))
                    {
                        var stateFile = Path.Combine(progressionDir, "progression_state.json");
                        var historyFile = Path.Combine(progressionDir, "xp_history.jsonl");
                        if (File.Exists(stateFile))
                        {
                            File.Delete(stateFile);
                            Debug.Log($"[ProgressionService] Deleted progression state: {stateFile}");
                        }
                        if (File.Exists(historyFile))
                        {
                            File.Delete(historyFile);
                        }
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

    private static List<XpHistoryEntry> LoadXpHistoryForPlayer(string historyPath, string playerId)
    {
        var history = new List<XpHistoryEntry>();
        if (!File.Exists(historyPath)) return history;

        var lines = File.ReadAllLines(historyPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JSONNode.Parse(line);
            var entryPlayerId = json["player_id"].Value;
            if (!string.IsNullOrEmpty(entryPlayerId) &&
                !string.Equals(entryPlayerId, playerId, StringComparison.Ordinal))
            {
                continue;
            }

            history.Add(new XpHistoryEntry(playerId, 0, "")
            {
                id = json["id"].Value,
                player_id = string.IsNullOrEmpty(entryPlayerId) ? playerId : entryPlayerId,
                timestamp = json["timestamp"].Value,
                xp_gained = json["xp_gained"].AsInt,
                source = json["source"].Value
            });
        }

        return history;
    }
}
