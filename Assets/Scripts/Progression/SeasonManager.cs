using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    private SeasonSaveData seasonData;
    private ISeasonBackend backend;
    private ProgressionUIController uiController;
    private ProgressionService progressionService;

    // 🔔 Event fired whenever backend data changes (UI listens to this)
    public event Action OnSeasonDataUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Initialize the progression service
        progressionService = ProgressionService.Instance;
        if (progressionService == null)
        {
            var progressionGo = new GameObject("ProgressionService");
            progressionService = progressionGo.AddComponent<ProgressionService>();
        }

        // Initialize the backend (using LocalSeasonBackend by default, can be swapped with ApiClient)
        backend = LocalSeasonBackend.Instance;
        if (backend == null)
        {
            Debug.LogError("[SeasonManager] Failed to initialize backend");
            return;
        }

        uiController = FindObjectOfType<ProgressionUIController>();

        // Create a new season from backend
        var teamNames = new List<string> { "Jets", "Hawks", "Sharks", "Bears", "Lions", "Giants", "Eagles" };
        backend.CreateSeason(teamNames, "YOU", data =>
        {
            seasonData = data;

            // Notify UI that data is ready
            OnSeasonDataUpdated?.Invoke();
            uiController?.OnSeasonDataReady();
        });
    }

    // --- Exposed Properties ---
    public int CurrentWeek => seasonData?.current_week ?? 0;
    public int TotalWeeks => seasonData?.total_weeks ?? 0;

    public bool CanSimulateWeek =>
        seasonData != null && seasonData.current_week < seasonData.total_weeks;

    public List<TeamSaveData> Teams => seasonData?.teams ?? new List<TeamSaveData>();
    public TeamSaveData PlayerTeam => seasonData?.teams?.FirstOrDefault(t => t.is_player_team);

    public int PlayerXP
    {
        get
        {
            var playerId = PlayerTeam?.player_id;
            if (string.IsNullOrEmpty(playerId)) return 0;

            var state = progressionService?.GetState(playerId, createIfMissing: false);
            return state?.current_xp ?? 0;
        }
    }

    public int PlayerRank
    {
        get
        {
            if (Teams == null || Teams.Count == 0) return 0;

            var sorted = Teams.OrderByDescending(t => t.stats.points)
                              .ThenByDescending(t => t.stats.wins)
                              .ToList();

            var playerIndex = sorted.FindIndex(t => t.is_player_team);
            return playerIndex >= 0 ? playerIndex + 1 : 0;
        }
    }

    public string PlayerTier
    {
        get
        {
            var playerId = PlayerTeam?.player_id;
            if (string.IsNullOrEmpty(playerId)) return "rookie";

            var state = progressionService?.GetState(playerId, createIfMissing: false);
            return state?.current_tier ?? "rookie";
        }
    }

    public TierData CurrentTierData
    {
        get
        {
            var playerId = PlayerTeam?.player_id;
            if (string.IsNullOrEmpty(playerId)) return null;

            return progressionService?.GetCurrentTier(playerId);
        }
    }

    public Dictionary<string, TierData> AllTiers
    {
        get
        {
            return progressionService?.GetAllTiers() ?? new Dictionary<string, TierData>();
        }
    }

    public List<XpHistoryEntry> XpHistoryEntries
    {
        get
        {
            var playerId = PlayerTeam?.player_id;
            if (string.IsNullOrEmpty(playerId)) return new List<XpHistoryEntry>();

            var state = progressionService?.GetState(playerId, createIfMissing: false);
            if (state?.xp_history == null) return new List<XpHistoryEntry>();

            return state.xp_history
                .Where(e => string.IsNullOrEmpty(e.player_id) ||
                            string.Equals(e.player_id, playerId, StringComparison.Ordinal))
                .ToList();
        }
    }

    public List<string> XpHistory
    {
        get
        {
            var playerId = PlayerTeam?.player_id;
            if (string.IsNullOrEmpty(playerId)) return new List<string>();

            var result = new List<string>();
            foreach (var entry in XpHistoryEntries)
            {
                result.Add($"{entry.timestamp}: +{entry.xp_gained} XP ({entry.source})");
            }
            return result;
        }
    }

    public int LastXpGained
    {
        get
        {
            var entries = XpHistoryEntries;
            if (entries.Count == 0) return 0;

            return entries[entries.Count - 1].xp_gained;
        }
    }

    /// <summary>
    /// True if the most recent match XP grant was a win (source: match_win).
    /// </summary>
    public bool WasLastMatchWin
    {
        get
        {
            var source = LastMatchXpSource;
            return !string.IsNullOrEmpty(source)
                && source.IndexOf("win", StringComparison.OrdinalIgnoreCase) >= 0
                && source.IndexOf("loss", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }

    public string LastMatchXpSource =>
        XpHistoryEntries.Count > 0 ? XpHistoryEntries[^1].source : null;

    // --- Backend Integration ---
    public void SimulateNextWeek(Action<SeasonSaveData> callback = null)
    {
        if (seasonData == null)
        {
            Debug.LogError("❌ Cannot simulate week: Season data not initialized");
            return;
        }

        if (backend == null)
        {
            Debug.LogError("❌ Cannot simulate week: Backend not initialized");
            return;
        }

        if (!CanSimulateWeek)
        {
            Debug.LogWarning($"Cannot simulate week: season complete ({CurrentWeek}/{TotalWeeks} weeks played).");
            return;
        }

        // Simulate the week asynchronously
        backend.SimulateWeek(seasonData, updatedData =>
        {
            seasonData = updatedData;
            Debug.Log($"✅ Week {seasonData.current_week} simulated.");
            
            // Notify UI
            OnSeasonDataUpdated?.Invoke();
            callback?.Invoke(seasonData);
        },
        error =>
        {
            Debug.LogError($"❌ SimulateWeek error: {error}");
        });
    }

    public void RefreshUI()
    {
        OnSeasonDataUpdated?.Invoke();
        uiController?.OnSeasonDataReady();
    }
}
