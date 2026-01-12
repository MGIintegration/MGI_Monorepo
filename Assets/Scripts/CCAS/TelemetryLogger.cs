using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Simplified Telemetry Logger (Phase 1)
/// Records one log per pack pull with emotional snapshot.
/// Stores data as JSON and exports frustration/satisfaction deltas to CSV.
/// </summary>
public class TelemetryLogger : MonoBehaviour
{
    public static TelemetryLogger Instance;

    private string logFilePath;
    private string csvDirPath;
    private const int MaxLogs = 1000;
    private const int MaxFileSizeKB = 512;

    [Header("Debug")]
    public bool verboseLogging = true;

    public event Action<PackPullLog> OnPullLogged;

    [Serializable]
    private class LogWrapper { public List<PackPullLog> logs = new(); }
    private LogWrapper cached = new();

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        string dir = Path.Combine(Application.persistentDataPath, "Telemetry");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        logFilePath = Path.Combine(dir, "pull_history.json");

        csvDirPath = Path.Combine(dir, "csv_exports");
        if (!Directory.Exists(csvDirPath)) Directory.CreateDirectory(csvDirPath);

        LoadCachedFile();
    }

    private void LoadCachedFile()
    {
        if (File.Exists(logFilePath))
        {
            try
            {
                cached = JsonConvert.DeserializeObject<LogWrapper>(File.ReadAllText(logFilePath)) ?? new LogWrapper();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Telemetry] Failed to read log file: {e.Message}");
                cached = new LogWrapper();
            }
        }
        else
        {
            SaveFile();
        }
    }

    // -------------------------------------------------------------------------
    // MAIN LOGGING API
    // -------------------------------------------------------------------------
    /// <summary>
    /// Logs a pack pull with Card objects (includes full card data).
    /// </summary>
    public void LogPull(string packTypeKey, string packName, int packCostCoins, List<Card> cards)
    {
        try
        {
            if (cards == null) cards = new();

            // Build per-card pull counts from existing pull_history BEFORE this pull.
            // This acts as the player collection + history for duplicate detection.
            var pullCounts = BuildCardPullCountsFromHistory();

            int totalXpGained = 0;
            int duplicateCount = 0;

            var ev = new PackPullLog
            {
                event_id = $"pull_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}".Substring(0, 30),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                session_id = PlayerPrefs.GetString("session_id", $"session_{SystemInfo.deviceUniqueIdentifier}_{DateTime.UtcNow:yyyyMMdd}"),
                player_id = PlayerPrefs.GetString("player_id", SystemInfo.deviceUniqueIdentifier),
                player_level = PlayerPrefs.GetInt("player_level", 1),
                pack_type = packTypeKey,
                pack_name = packName,
                cost_coins = packCostCoins
            };

            // Store card data
            ev.pulled_cards = new List<CardData>();
            ev.pull_results = new List<string>(); // Keep for backward compatibility
            
            foreach (var card in cards)
            {
                if (card != null)
                {
                    string rarity = card.GetRarityString();
                    string uid = card.uid ?? string.Empty;
                    
                    int previousPulls = 0;
                    if (!string.IsNullOrEmpty(uid))
                        pullCounts.TryGetValue(uid, out previousPulls);

                    // Duplicate if we've seen this card UID before (including earlier in this same pull).
                    bool isDuplicate = !string.IsNullOrEmpty(uid) && previousPulls > 0;

                    // Update counts to include this pull.
                    if (!string.IsNullOrEmpty(uid))
                        pullCounts[uid] = previousPulls + 1;

                    int xpForThisCard = isDuplicate ? GetDuplicateXpForRarity(rarity) : 0;
                    if (isDuplicate)
                    {
                        duplicateCount++;
                        totalXpGained += xpForThisCard;
                    }

                    ev.pulled_cards.Add(new CardData
                    {
                        uid = uid,
                        name = card.name ?? "",
                        team = card.team ?? "",
                        element = card.element ?? "",
                        rarity = rarity,
                        position5 = card.position5 ?? "",
                        is_duplicate = isDuplicate,
                        xp_gained = xpForThisCard,
                        total_pulls_for_card = previousPulls + 1,
                        duplicate_pulls_for_card = Mathf.Max(0, previousPulls + 1 - 1)
                    });
                    ev.pull_results.Add(rarity); // Backward compatibility
                }
            }

            // Update player's XP pool based on duplicates in this pull
            int previousXp = PlayerPrefs.GetInt("player_xp", 0);
            int newTotalXp = previousXp + Mathf.Max(0, totalXpGained);
            PlayerPrefs.SetInt("player_xp", newTotalXp);
            PlayerPrefs.Save();

            ev.total_xp_gained = totalXpGained;
            ev.duplicate_count = duplicateCount;
            ev.player_xp_after = newTotalXp;

            // Emotional snapshot (after pull)
            if (EmotionalStateManager.Instance != null)
            {
                var (fr, sa) = EmotionalStateManager.Instance.Snapshot();
                ev.satisfaction_after = sa;
                ev.frustration_after = fr;
            }

            // Save to cache
            cached.logs.Add(ev);
            if (cached.logs.Count > MaxLogs)
                cached.logs.RemoveRange(0, cached.logs.Count - MaxLogs);

            SaveFile();
            ExportEmotionalStateCSV(ev);

            if (verboseLogging)
            {
                // Detailed per-card duplicate + XP info for debugging.
                var cardInfos = ev.pulled_cards.Select(c =>
                    $"{(string.IsNullOrEmpty(c.name) ? "Unknown Card" : c.name)} [{c.rarity}] " +
                    $"dup={c.is_duplicate} xp={c.xp_gained} pulls={c.total_pulls_for_card} dup_pulls={c.duplicate_pulls_for_card}");

                Debug.Log(
                    $"[Telemetry] Logged {packTypeKey} → {string.Join("; ", cardInfos)} | " +
                    $"duplicates={ev.duplicate_count} xp_gained={ev.total_xp_gained} xp_total={ev.player_xp_after} | " +
                    $"logs={cached.logs.Count}"
                );
            }

            OnPullLogged?.Invoke(ev);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Telemetry] Error during LogPull: {e.Message}");
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility (rarities only).
    /// </summary>
    public void LogPull(string packTypeKey, string packName, int packCostCoins, List<string> rarities)
    {
        try
        {
            if (rarities == null) rarities = new();

            var ev = new PackPullLog
            {
                event_id = $"pull_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}".Substring(0, 30),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                session_id = PlayerPrefs.GetString("session_id", $"session_{SystemInfo.deviceUniqueIdentifier}_{DateTime.UtcNow:yyyyMMdd}"),
                player_id = PlayerPrefs.GetString("player_id", SystemInfo.deviceUniqueIdentifier),
                player_level = PlayerPrefs.GetInt("player_level", 1),
                pack_type = packTypeKey,
                pack_name = packName,
                cost_coins = packCostCoins,
                pull_results = new List<string>(rarities),
                pulled_cards = new List<CardData>() // Empty for legacy calls
            };

            // Emotional snapshot (after pull)
            if (EmotionalStateManager.Instance != null)
            {
                var (fr, sa) = EmotionalStateManager.Instance.Snapshot();
                ev.satisfaction_after = sa;
                ev.frustration_after = fr;
            }

            // Save to cache
            cached.logs.Add(ev);
            if (cached.logs.Count > MaxLogs)
                cached.logs.RemoveRange(0, cached.logs.Count - MaxLogs);

            SaveFile();
            ExportEmotionalStateCSV(ev);

            if (verboseLogging)
            {
                Debug.Log($"[Telemetry] Logged {packTypeKey} → [{string.Join(", ", rarities)}] | logs={cached.logs.Count}");
            }

            OnPullLogged?.Invoke(ev);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Telemetry] Error during LogPull: {e.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // HOOK LOGGING (optional, minimal)
    // -------------------------------------------------------------------------
    public void LogHookExecution(string hookId, bool fired, string reasonIfBlocked, string context = null)
    {
        if (verboseLogging)
            Debug.Log($"[Hook] {hookId} fired={fired} reason={reasonIfBlocked ?? "-"} ctx={context}");
    }

    // -------------------------------------------------------------------------
    // CSV EXPORT
    // -------------------------------------------------------------------------
    private void ExportEmotionalStateCSV(PackPullLog ev)
    {
        string csvPath = Path.Combine(csvDirPath, "PHASE_1_EMOTIONAL_STATE_LOG.csv");

        if (!File.Exists(csvPath))
        {
            string header = "log_id,timestamp,session_id,player_id,event_type,frustration_after,satisfaction_after,frustration_delta,satisfaction_delta";
            File.WriteAllText(csvPath, header + "\n");
        }

        var (frAfter, saAfter) = EmotionalStateManager.Instance?.Snapshot() ?? (0f, 0f);
        float frDelta = EmotionalStateManager.Instance?.GetLastFrustrationDelta() ?? 0f;
        float saDelta = EmotionalStateManager.Instance?.GetLastSatisfactionDelta() ?? 0f;

        var rowValues = new List<string>
        {
            ev.event_id,
            ev.timestamp.ToString(),
            ev.session_id,
            ev.player_id,
            "outcome",
            frAfter.ToString("F2"),
            saAfter.ToString("F2"),
            frDelta.ToString("F2"),
            saDelta.ToString("F2")
        };

        File.AppendAllText(csvPath, string.Join(",", rowValues) + "\n");
    }

    // -------------------------------------------------------------------------
    // FILE HANDLING
    // -------------------------------------------------------------------------
    private void SaveFile()
    {
        try
        {
            string json = JsonConvert.SerializeObject(cached, Formatting.Indented);

            if ((System.Text.Encoding.UTF8.GetByteCount(json) / 1024f) > MaxFileSizeKB && cached.logs.Count > 100)
                cached.logs.RemoveRange(0, 100);

            File.WriteAllText(logFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Telemetry] Write failed: {e.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // DUPLICATE DETECTION HELPERS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a map of card UID → total pulls so far (from cached pull_history).
    /// Used for both duplicate detection and per-card dupe counters.
    /// </summary>
    private Dictionary<string, int> BuildCardPullCountsFromHistory()
    {
        var dict = new Dictionary<string, int>();
        if (cached?.logs == null) return dict;

        foreach (var log in cached.logs)
        {
            if (log?.pulled_cards == null) continue;
            foreach (var card in log.pulled_cards)
            {
                if (card == null) continue;
                if (string.IsNullOrEmpty(card.uid)) continue;

                if (!dict.TryGetValue(card.uid, out var count))
                    dict[card.uid] = 1;
                else
                    dict[card.uid] = count + 1;
            }
        }
        return dict;
    }

    /// <summary>
    /// Returns XP for a duplicate card based on its rarity, using Phase 1 Part 4 tuning.
    /// Falls back to default values if config is missing.
    /// </summary>
    private int GetDuplicateXpForRarity(string rarity)
    {
        string r = (rarity ?? "common").ToLowerInvariant();

        // Try JSON-configured values first (phase1_config.json)
        var cfg = DropConfigManager.Instance?.config;
        var dxp = cfg?.duplicate_xp;

        if (dxp != null)
        {
            return r switch
            {
                "uncommon"  => dxp.uncommon_duplicate_xp,
                "rare"      => dxp.rare_duplicate_xp,
                "epic"      => dxp.epic_duplicate_xp,
                "legendary" => dxp.legendary_duplicate_xp,
                _           => dxp.common_duplicate_xp
            };
        }

        // Fallback to design defaults if config block is missing
        return r switch
        {
            "uncommon"  => 10,
            "rare"      => 25,
            "epic"      => 50,
            "legendary" => 100,
            _           => 5
        };
    }

    [ContextMenu("Clear Pull History")]
    public void ClearLogFile()
    {
        cached = new LogWrapper();
        SaveFile();
        Debug.Log("🗑 [Telemetry] Cleared pull_history.json");
    }

    public List<PackPullLog> GetRecent(int count)
    {
        if (cached?.logs == null || cached.logs.Count == 0)
            return new List<PackPullLog>();

        count = Mathf.Max(1, count);
        int start = Mathf.Max(0, cached.logs.Count - count);
        return cached.logs.GetRange(start, cached.logs.Count - start);
    }

    // -------------------------------------------------------------------------
    // LOG STRUCTURE
    // -------------------------------------------------------------------------
    [Serializable]
    public class PackPullLog
    {
        public string event_id;
        public long timestamp;
        public string session_id;
        public string player_id;
        public int player_level;

        public string pack_type;
        public string pack_name;
        public int cost_coins;

        public List<string> pull_results; // Backward compatibility - rarity strings
        public List<CardData> pulled_cards; // Full card data
        
        // Emotional snapshot AFTER pull
        public float satisfaction_after;
        public float frustration_after;

        // Duplicate conversion summary for this pull
        public int total_xp_gained;
        public int duplicate_count;
        public int player_xp_after;
    }

    [Serializable]
    public class CardData
    {
        public string uid;
        public string name;
        public string team;
        public string element;
        public string rarity;
        public string position5;

        // Duplicate conversion flags
        public bool is_duplicate;
        public int xp_gained;

        // Pull counters
        public int total_pulls_for_card;
        public int duplicate_pulls_for_card;
    }
}
