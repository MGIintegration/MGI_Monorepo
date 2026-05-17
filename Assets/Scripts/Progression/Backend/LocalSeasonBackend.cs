using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SimpleJSON;

public class LocalSeasonBackend : MonoBehaviour, ISeasonBackend
{
    private static LocalSeasonBackend _instance;
    public static LocalSeasonBackend Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LocalSeasonBackend>();
                if (_instance == null)
                {
                    var go = new GameObject("LocalSeasonBackend");
                    _instance = go.AddComponent<LocalSeasonBackend>();
                }
            }
            return _instance;
        }
    }

    private ProgressionService _progressionService;
    private string _currentSeasonPath;

    // XP Reward Rules
    private static readonly int XP_MATCH_PLAYED = 5;
    private static readonly int XP_WIN = 10;
    private static readonly int XP_LOSS = 2;
    private static readonly int XP_1ST_PLACE = 50;
    private static readonly int XP_2ND_PLACE = 30;
    private static readonly int XP_3RD_PLACE = 15;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
{
    _progressionService = ProgressionService.Instance;
    if (_progressionService == null)
    {
        Debug.LogError("[LocalSeasonBackend] ProgressionService not found!");
    }
    else
    {
        // Clear all old player progression data to start fresh session
        _progressionService.ClearAllPlayerProgression();
    }

    SaveProgressionDataToJson();
}

    #region ISeasonBackend Implementations

    public void CreateSeason(List<string> teamNames, string playerTeamName, Action<SeasonSaveData> onSuccess, Action<string> onError = null)
    {
        try
        {
            var seasonData = new SeasonSaveData
            {
                season_id = Guid.NewGuid().ToString(),
                current_week = 0,
                total_weeks = 10,
                teams = new List<TeamSaveData>()
            };

            // Add AI teams
            foreach (var teamName in teamNames)
            {
                var team = new TeamSaveData
                {
                    team_id = Guid.NewGuid().ToString(),
                    player_id = Guid.NewGuid().ToString(),
                    team_name = teamName,
                    rating = 1000,
                    is_player_team = false,
                    rank = 0,
                    stats = new TeamStatsSaveData
                    {
                        wins = 0,
                        losses = 0,
                        points = 0,
                        total_matches = 0
                    },
                    progression = new TeamProgressionSaveData
                    {
                        total_xp = 0,
                        current_level = 0,
                        tier = "rookie",
                        xp_history = new List<Dictionary<string, object>>()
                    }
                };
                seasonData.teams.Add(team);
            }

            // Add player team
            var playerTeam = new TeamSaveData
            {
                team_id = Guid.NewGuid().ToString(),
                player_id = "player_1",
                team_name = playerTeamName,
                rating = 1000,
                is_player_team = true,
                rank = 0,
                stats = new TeamStatsSaveData
                {
                    wins = 0,
                    losses = 0,
                    points = 0,
                    total_matches = 0
                },
                progression = new TeamProgressionSaveData
                {
                    total_xp = 0,
                    current_level = 0,
                    tier = "rookie",
                    xp_history = new List<Dictionary<string, object>>()
                }
            };
            seasonData.teams.Add(playerTeam);

            // Save season data
            SaveSeasonData(seasonData);

            // Initialize progression for player team
            var progressionState = _progressionService?.GetState(playerTeam.player_id, createIfMissing: true);
            if (progressionState != null)
            {
                Debug.Log($"[LocalSeasonBackend] Initialized progression for player: {playerTeam.player_id}");
            }
            _progressionService?.GetState(playerTeam.player_id, createIfMissing: true);
            Debug.Log($"[LocalSeasonBackend] Season created: {seasonData.season_id} with {seasonData.teams.Count} teams");
            onSuccess?.Invoke(seasonData);
        }
        catch (Exception ex)
        {
            var error = $"[LocalSeasonBackend] CreateSeason failed: {ex.Message}";
            Debug.LogError(error);
            onError?.Invoke(error);
        }
    }


    public void SimulateWeek(SeasonSaveData currentSeason, Action<SeasonSaveData> onSuccess, Action<string> onError = null)
    {
        try
        {
            if (currentSeason == null || currentSeason.teams == null)
                throw new ArgumentNullException("currentSeason or currentSeason.teams is null");

            currentSeason.current_week++;

            var random = new System.Random();
            var playerTeam = currentSeason.teams.FirstOrDefault(t => t.is_player_team);
            bool playerWonThisWeek = false;

            // Simulate matches for all teams
            foreach (var team in currentSeason.teams)
            {
                if (team == null || team.stats == null) continue;

                if (!team.is_player_team)
                {
                    if (random.NextDouble() > 0.5) team.stats.wins++;
                    else team.stats.losses++;
                    team.stats.total_matches++;
                    team.stats.points = team.stats.wins * 2;
                }
                else
                {
                    // Player team match
                    bool playerWins = random.NextDouble() > 0.5;
                    if (playerWins)
                    {
                        team.stats.wins++;
                        playerWonThisWeek = true;
                    }
                    else
                    {
                        team.stats.losses++;
                    }
                    team.stats.total_matches++;
                    team.stats.points = team.stats.wins * 2; // Simple points calculation
                }
            }

            // Award XP to player team (stub implementation)
            if (playerTeam != null)
            {
                if (string.IsNullOrEmpty(playerTeam.player_id))
                {
                    Debug.LogWarning("[LocalSeasonBackend] Player team has null/empty player_id");
                }
                else
                {
                    int baseXp = random.Next(5, 20); // 5-20 XP per week
                    _progressionService?.AddXp(playerTeam.player_id, baseXp, "match_played", Guid.NewGuid().ToString(), 1f);
                }
            }
            else
            {
                Debug.LogWarning("[LocalSeasonBackend] No player team found in season");
            }

            // Save updated season
            SaveSeasonData(currentSeason);

            Debug.Log($"[LocalSeasonBackend] Week {currentSeason.current_week} simulated");
            onSuccess?.Invoke(currentSeason);
        }
        catch (Exception ex)
        {
            var error = $"[LocalSeasonBackend] SimulateWeek failed: {ex.Message}";
            Debug.LogError(ex);
            onError?.Invoke(ex.Message);
        }
    }

  
    public void GetPlayerProgression(string playerId, Action<PlayerProgressionSaveData> onSuccess, Action<string> onError = null)
    {
        try
        {
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentException("playerId is null or empty");
            }

            var state = _progressionService?.GetState(playerId, createIfMissing: false);
            if (state == null)
            {
                throw new Exception($"Progression state not found for player: {playerId}");
            }

            // Convert to legacy format
            var legacyData = ConvertToLegacyFormat(state);
            onSuccess?.Invoke(legacyData);
        }
        catch (Exception ex)
        {
            var error = $"[LocalSeasonBackend] GetPlayerProgression failed: {ex.Message}";
            Debug.LogError(error);
            onError?.Invoke(error);
        }
    }


    public void GetLocalProgression(string playerId, Action<PlayerProgressionState> onSuccess, Action<string> onError = null)
    {
        try
        {
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentException("playerId is null or empty");
            }

            var state = _progressionService?.GetState(playerId, createIfMissing: false);
            if (state == null)
            {
                throw new Exception($"Progression state not found for player: {playerId}");
            }

            onSuccess?.Invoke(state);
        }
        catch (Exception ex)
        {
            var error = $"[LocalSeasonBackend] GetLocalProgression failed: {ex.Message}";
            Debug.LogError(error);
            onError?.Invoke(error);
        }
    }

    public void AddPlayerXp(string playerId, int xpAmount, string source, Action<PlayerProgressionState> onSuccess, Action<string> onError = null)
    {
        try
        {
            _progressionService?.AddXp(
                playerId,
                xpAmount,
                source,
                Guid.NewGuid().ToString(), // unique eventId
                1f // default multiplier
            );

            SaveProgressionDataToJson();

            var state = _progressionService?.GetState(playerId, createIfMissing: false);
            onSuccess?.Invoke(state);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalSeasonBackend] AddPlayerXp failed: {ex}");
            onError?.Invoke(ex.Message);
        }
    }

    /// <summary>
    /// Awards season-end XP rewards based on final standings
    /// Provides bonus XP for 1st (50), 2nd (30), 3rd (15) place finishes
    /// </summary>
    public void AwardSeasonRewards(SeasonSaveData seasonData, Action<string> onSuccess, Action<string> onError = null)
    {
        try
        {
            if (seasonData == null || seasonData.teams == null || seasonData.teams.Count == 0)
            {
                throw new ArgumentException("Invalid season data");
            }

            // Sort teams by points (ranking)
            var rankedTeams = seasonData.teams
                .Where(t => t != null && t.is_player_team)
                .OrderByDescending(t => t.stats.points)
                .ToList();

            if (rankedTeams.Count == 0)
            {
                throw new Exception("No player team found in season data");
            }

            var playerTeam = rankedTeams.First();
            int placement = 1;
            int xpReward = 0;

            // Determine placement and reward
            // This is simplified - in a real scenario, you'd rank all teams
            var allTeamsByPoints = seasonData.teams
                .Where(t => t != null && t.stats != null)
                .OrderByDescending(t => t.stats.points)
                .ToList();

            placement = allTeamsByPoints.FindIndex(t => t.team_id == playerTeam.team_id) + 1;

            if (placement == 1)
            {
                xpReward = XP_1ST_PLACE;
            }
            else if (placement == 2)
            {
                xpReward = XP_2ND_PLACE;
            }
            else if (placement == 3)
            {
                xpReward = XP_3RD_PLACE;
            }
            else
            {
                // No reward for 4th place or lower
                Debug.Log($"[LocalSeasonBackend] Player team finished {placement}th - no season reward");
                onSuccess?.Invoke("No reward for this placement");
                return;
            }

            // Award the XP
            _progressionService?.AddXp(
                playerTeam.player_id,
                xpReward,
                "season_reward",
                Guid.NewGuid().ToString(),
                1f
            );
            SaveProgressionDataToJson();
            Debug.Log($"[LocalSeasonBackend] Season reward awarded: {xpReward} XP for {placement} place finish");
            onSuccess?.Invoke($"Awarded {xpReward} XP for {placement} place finish");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalSeasonBackend] AwardSeasonRewards failed: {ex}");
            onError?.Invoke(ex.Message);
        }
    }

    #endregion

    #region Helper Methods

    private PlayerProgressionSaveData ConvertToLegacyFormat(PlayerProgressionState state)
    {
        var legacy = new PlayerProgressionSaveData
        {
            player_id = state.player_id,
            current_xp = state.current_xp,
            current_tier = state.current_tier,
            tier_progression = _progressionService?.GetAllTiers() ?? new Dictionary<string, TierData>(),
            xp_history = new List<XpHistoryEntry>()
        };

        foreach (var entry in state.xp_history)
        {
            legacy.xp_history.Add(new XpHistoryEntry
            {
                timestamp = entry.timestamp,
                xp_gained = entry.xp_gained,
                source = entry.source
            });
        }

        return legacy;
    }

    private void SaveSeasonData(SeasonSaveData seasonData)
    {
        try
        {
            var seasonDir = Path.Combine(FilePathResolver.GetRoot(), "seasons");
            Directory.CreateDirectory(seasonDir);

            var seasonPath = Path.Combine(seasonDir, $"{seasonData.season_id}.json");
            var json = new JSONObject();

            json["season_id"] = seasonData.season_id;
            json["current_week"] = seasonData.current_week;
            json["total_weeks"] = seasonData.total_weeks;

            var teamsArray = new JSONArray();
            foreach (var team in seasonData.teams)
            {
                var teamJson = new JSONObject
                {
                    ["team_id"] = team.team_id,
                    ["player_id"] = team.player_id,
                    ["team_name"] = team.team_name,
                    ["rating"] = team.rating,
                    ["is_player_team"] = team.is_player_team,
                    ["wins"] = team.stats.wins,
                    ["losses"] = team.stats.losses,
                    ["points"] = team.stats.points
                };
                teamsArray.Add(teamJson);
            }
            json["teams"] = teamsArray;

            File.WriteAllText(seasonPath, json.ToString(2));
            _currentSeasonPath = seasonPath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalSeasonBackend] Failed to save season: {ex}");
        }
    }
    private SeasonSaveData LoadSeasonData(string seasonId)
    {
        try
        {
            var seasonDir = Path.Combine(FilePathResolver.GetRoot(), "seasons");
            var seasonPath = Path.Combine(seasonDir, $"{seasonId}.json");

            if (!File.Exists(seasonPath))
            {
                return null;
            }

            var json = JSONNode.Parse(File.ReadAllText(seasonPath));
            var seasonData = new SeasonSaveData
            {
                season_id = json["season_id"].Value,
                current_week = json["current_week"].AsInt,
                total_weeks = json["total_weeks"].AsInt,
                teams = new List<TeamSaveData>()
            };

            foreach (KeyValuePair<string, JSONNode> kvp in json["teams"])
            {
                var teamNode = kvp.Value;
                var team = new TeamSaveData
                {
                    team_id = teamNode["team_id"].Value,
                    player_id = teamNode["player_id"].Value,
                    team_name = teamNode["team_name"].Value,
                    rating = teamNode["rating"].AsInt,
                    is_player_team = teamNode["is_player_team"].AsBool,
                    stats = new TeamStatsSaveData
                    {
                        wins = teamNode["wins"].AsInt,
                        losses = teamNode["losses"].AsInt,
                        points = teamNode["points"].AsInt,
                        total_matches = teamNode["wins"].AsInt + teamNode["losses"].AsInt
                    },
                    progression = new TeamProgressionSaveData()
                };
                seasonData.teams.Add(team);
            }

            return seasonData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalSeasonBackend] Failed to load season data: {ex}");
            return null;
        }
    }
    private void SaveProgressionDataToJson()
    {
    try
    {
        var dir = Path.Combine(Application.streamingAssetsPath, "Progression");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var allStates = _progressionService?.GetAllStates()?.Values.ToList()
                        ?? new List<PlayerProgressionState>();

        // ---------------------------
        // xp_history_schema.json
        // ---------------------------
        var xpArray = new JSONArray();

        foreach (var state in allStates)
        {
            if (state?.xp_history == null) continue;

            foreach (var entry in state.xp_history)
            {
                var e = new JSONObject
                {
                    ["id"] = entry.id,
                    ["player_id"] = entry.player_id,
                    ["timestamp"] = entry.timestamp,
                    ["xp_gained"] = entry.xp_gained,
                    ["source"] = entry.source
                };

                xpArray.Add(e);
            }
        }

        File.WriteAllText(
            Path.Combine(dir, "xp_history_schema.json"),
            xpArray.ToString(2)
        );

        // ---------------------------
        // progression_state_schema.json
        // ---------------------------
        var progArray = new JSONArray();

        foreach (var state in allStates)
        {
            if (state == null) continue;

            var s = new JSONObject
            {
                ["player_id"] = state.player_id,
                ["current_xp"] = state.current_xp,
                ["current_tier"] = state.current_tier
            };

            var xpArr = new JSONArray();

            if (state.xp_history != null)
            {
                foreach (var entry in state.xp_history)
                {
                    var e = new JSONObject
                    {
                        ["id"] = entry.id,
                        ["player_id"] = entry.player_id,
                        ["timestamp"] = entry.timestamp,
                        ["xp_gained"] = entry.xp_gained,
                        ["source"] = entry.source
                    };

                    xpArr.Add(e);
                }
            }

            s["xp_history"] = xpArr;
            progArray.Add(s);
        }

        File.WriteAllText(
            Path.Combine(dir, "progression_state_schema.json"),
            progArray.ToString(2)
        );

        Debug.Log("[LocalSeasonBackend] Progression data saved to JSON.");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[LocalSeasonBackend] Failed to save progression data: {ex}");
    }
}
    /// <summary>
    /// Calculates the player tier based on current XP using the tier progression structure
    /// Tier progression:
    /// - rookie: 0-50 XP
    /// - pro: 51-100 XP
    /// - all_star: 101-150 XP
    /// - legend: 151-1000 XP
    /// </summary>
    private string CalculateTierFromXp(int currentXp)
    {
        if (currentXp >= 0 && currentXp <= 50)
            return "rookie";
        else if (currentXp >= 51 && currentXp <= 100)
            return "pro";
        else if (currentXp >= 101 && currentXp <= 150)
            return "all_star";
        else if (currentXp >= 151 && currentXp <= 1000)
            return "legend";
        
        // Default fallback
        return "rookie";
    }

    /// <summary>
    /// Gets the tier data for a given tier name
    /// Returns the tier configuration including min/max XP and unlock features
    /// </summary>
    private TierData GetTierDataByName(string tierName)
    {
        return tierName switch
        {
            "rookie" => new TierData
            {
                min_xp = 0,
                max_xp = 50,
                display_name = "Rookie",
                unlock_features = new List<string> { "Basic Arena Access", "Starter Pack" }
            },
            "pro" => new TierData
            {
                min_xp = 51,
                max_xp = 100,
                display_name = "Pro",
                unlock_features = new List<string> { "Elite Arena", "Pro Training Facility" }
            },
            "all_star" => new TierData
            {
                min_xp = 101,
                max_xp = 150,
                display_name = "All-Star",
                unlock_features = new List<string> { "Star Arena", "Star Training Facility" }
            },
            "legend" => new TierData
            {
                min_xp = 151,
                max_xp = 1000,
                display_name = "Legend",
                unlock_features = new List<string> { "Legendary Arena", "Hall of Fame Access" }
            },
            _ => new TierData
            {
                min_xp = 0,
                max_xp = 50,
                display_name = "Rookie",
                unlock_features = new List<string> { "Basic Arena Access", "Starter Pack" }
            }
        };
    }

    /// <summary>
    /// Gets the full tier progression dictionary for the player progression system
    /// </summary>
    private Dictionary<string, TierData> GetTierProgressionData()
    {
        return new Dictionary<string, TierData>
        {
            ["rookie"] = new TierData
            {
                min_xp = 0,
                max_xp = 50,
                display_name = "Rookie",
                unlock_features = new List<string> { "Basic Arena Access", "Starter Pack" }
            },
            ["pro"] = new TierData
            {
                min_xp = 51,
                max_xp = 100,
                display_name = "Pro",
                unlock_features = new List<string> { "Elite Arena", "Pro Training Facility" }
            },
            ["all_star"] = new TierData
            {
                min_xp = 101,
                max_xp = 150,
                display_name = "All-Star",
                unlock_features = new List<string> { "Star Arena", "Star Training Facility" }
            },
            ["legend"] = new TierData
            {
                min_xp = 151,
                max_xp = 1000,
                display_name = "Legend",
                unlock_features = new List<string> { "Legendary Arena", "Hall of Fame Access" }
            }
        };
    }
    #endregion
}
