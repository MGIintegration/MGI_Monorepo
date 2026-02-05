using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    private SeasonSaveData seasonData;
    private ApiClient apiClient;
    private ProgressionUIController uiController;

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
        apiClient = gameObject.AddComponent<ApiClient>();
        uiController = FindObjectOfType<ProgressionUIController>();

        // Create a new season from backend
        StartCoroutine(apiClient.PostCreateSeason(data =>
        {
            seasonData = data;

            // Notify UI that data is ready
            OnSeasonDataUpdated?.Invoke();
            uiController?.OnSeasonDataReady();
        }));
    }

    // --- Exposed Properties ---
    public int CurrentWeek => seasonData?.current_week ?? 0;
    public int TotalWeeks => seasonData?.total_weeks ?? 0;

    public List<TeamSaveData> Teams => seasonData?.teams ?? new List<TeamSaveData>();
    public TeamSaveData PlayerTeam => seasonData?.teams?.FirstOrDefault(t => t.is_player_team);

    public int PlayerXP => ApiClient.Instance?.PlayerProgressionSaveData?.current_xp ?? 0;

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
            return ApiClient.Instance?.PlayerProgressionSaveData?.current_tier ?? "rookie";
        }
    }

    public TierData CurrentTierData
    {
        get
        {
            var prog = ApiClient.Instance?.PlayerProgressionSaveData;
            if (prog == null || prog.tier_progression == null) return null;

            if (prog.tier_progression.TryGetValue(PlayerTier, out var tierData))
                return tierData;

            return null;
        }
    }

    public Dictionary<string, TierData> AllTiers
    {
        get
        {
            return ApiClient.Instance?.PlayerProgressionSaveData?.tier_progression 
                ?? new Dictionary<string, TierData>();
        }
    }



    public List<string> XpHistory
    {
        get
        {
            var xpData = ApiClient.Instance?.PlayerProgressionSaveData?.xp_history;
            if (xpData == null) return new List<string>();

            return xpData.Select(e => $"{e.timestamp}: +{e.xp_gained} XP ({e.source})").ToList();
        }
    }

    // --- API Integration ---
    public void SimulateNextWeek(Action<SeasonSaveData> callback = null)
    {
        if (seasonData == null)
        {
            Debug.LogError("❌ Cannot simulate week: Season data not initialized");
            return;
        }

        StartCoroutine(SimulateWeekCoroutine(callback));
    }

    private IEnumerator SimulateWeekCoroutine(Action<SeasonSaveData> callback)
    {
        yield return apiClient.PostSimulateWeek(seasonData, data =>
        {
            seasonData = data;
            Debug.Log($"Week {seasonData.current_week} simulated.");
        });

        // Fetch updated XP history
        var playerId = PlayerTeam?.player_id;

        yield return apiClient.GetPlayerProgression(playerId, () =>
        {
            OnSeasonDataUpdated?.Invoke();  
            callback?.Invoke(seasonData);
        });
    }


    public void RefreshUI()
    {

        OnSeasonDataUpdated?.Invoke();
        uiController?.OnSeasonDataReady();
    }
}
