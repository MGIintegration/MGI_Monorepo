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
    }

    
    public void CreateSeason(List<string> teamNames, string playerTeamName, Action<SeasonSaveData> onSuccess, Action<string> onError = null)
    {
        try
        {
            var seasonData = new SeasonSaveData
            {
                season_id = Guid.NewGuid().ToString(),
                current_week = 1,
                total_weeks = 18,
                teams = new List<TeamSaveData>()
            };

            // Create AI team data
            for (int i = 0; i < teamNames.Count; i++)
            {
                var team = new TeamSaveData
                {
                    team_id = Guid.NewGuid().ToString(),
                    player_id = Guid.NewGuid().ToString(),
                    team_name = teamNames[i],
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

            // Create player team
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
            if (currentSeason == null)
            {
                throw new ArgumentNullException(nameof(currentSeason));
            }

            if (currentSeason.teams == null)
            {
                throw new ArgumentException("currentSeason.teams is null");
            }

            // Advance to next week
            currentSeason.current_week++;

            // Stub: Simulate basic match results (randomly assign wins/losses)
            var random = new System.Random();
            foreach (var team in currentSeason.teams)
            {
                if (team == null)
                {
                    Debug.LogWarning("[LocalSeasonBackend] Found null team in teams list");
                    continue;
                }

                if (team.stats == null)
                {
                    Debug.LogWarning($"[LocalSeasonBackend] team.stats is null for team {team.team_name}");
                    continue;
                }

                if (!team.is_player_team)
                {
                    // Random match outcome
                    if (random.NextDouble() > 0.5)
                    {
                        team.stats.wins++;
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
            if (currentSeason.teams.FirstOrDefault(t => t != null && t.is_player_team) is TeamSaveData playerTeam)
            {
                if (string.IsNullOrEmpty(playerTeam.player_id))
                {
                    Debug.LogWarning("[LocalSeasonBackend] Player team has null/empty player_id");
                }
                else
                {
                    int baseXp = random.Next(5, 20); // 5-20 XP per week
                    _progressionService?.AddXp(playerTeam.player_id, baseXp, "match_played");
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
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentException("playerId is null or empty");
            }

            if (xpAmount <= 0)
            {
                throw new ArgumentException("xpAmount must be positive");
            }

            _progressionService?.AddXp(playerId, xpAmount, source);
            var state = _progressionService?.GetState(playerId, createIfMissing: false);

            if (state != null)
            {
                onSuccess?.Invoke(state);
            }
            else
            {
                throw new Exception("Failed to retrieve updated progression state");
            }
        }
        catch (Exception ex)
        {
            var error = $"[LocalSeasonBackend] AddPlayerXp failed: {ex.Message}";
            Debug.LogError(error);
            onError?.Invoke(error);
        }
    }


    private PlayerProgressionSaveData ConvertToLegacyFormat(PlayerProgressionState newState)
    {
        var legacyData = new PlayerProgressionSaveData
        {
            player_id = newState.player_id,
            current_xp = newState.current_xp,
            current_tier = newState.current_tier,
            tier_progression = _progressionService?.GetAllTiers() ?? new Dictionary<string, TierData>(),
            xp_history = new List<XPHistoryEntry>()
        };

        foreach (var entry in newState.xp_history)
        {
            legacyData.xp_history.Add(new XPHistoryEntry
            {
                timestamp = entry.timestamp,
                xp_gained = entry.xp_gained,
                source = entry.source,
                facility_multiplier = 1.0f,
                coaching_bonus = 0.0f
            });
        }

        return legacyData;
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
                var teamJson = new JSONObject();
                teamJson["team_id"] = team.team_id;
                teamJson["player_id"] = team.player_id;
                teamJson["team_name"] = team.team_name;
                teamJson["rating"] = team.rating;
                teamJson["is_player_team"] = team.is_player_team;
                teamJson["wins"] = team.stats.wins;
                teamJson["losses"] = team.stats.losses;
                teamJson["points"] = team.stats.points;
                teamsArray.Add(teamJson);
            }
            json["teams"] = teamsArray;

            File.WriteAllText(seasonPath, json.ToString(2));
            _currentSeasonPath = seasonPath;
            Debug.Log($"[LocalSeasonBackend] Season saved to {seasonPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalSeasonBackend] Failed to save season data: {ex}");
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
}
