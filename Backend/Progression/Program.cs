using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Collections.Concurrent;
using System.IO;
using FMGSeasonProgression;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var active_seasons = new ConcurrentDictionary<string, SeasonController>();
var player_progressions = new ConcurrentDictionary<string, PlayerProgression>();

Directory.CreateDirectory("data/seasons");
Directory.CreateDirectory("data/progression");

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

app.MapPost("/seasons", (CreateSeasonRequest request) =>
{
    if (request.teams == null || request.teams.Count == 0)
        return Results.BadRequest("No teams provided.");

    string season_id = Guid.NewGuid().ToString();
    var teams = new List<TeamData>();

    foreach (var d in request.teams)
    {
        string name = d.ContainsKey("team_name") ? d["team_name"].ToString() ?? "Unnamed" : "Unnamed";
        string player_id = d.ContainsKey("player_id") ? d["player_id"].ToString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
        int rating = 1000;
        if (d.ContainsKey("rating") && int.TryParse(d["rating"].ToString(), out int parsed))
            rating = parsed;

        teams.Add(new TeamData
        {
            team_id = Guid.NewGuid().ToString(),
            player_id = player_id,
            team_name = name,
            rating = rating,
            is_player_team = name == request.player_team_name
        });

        player_progressions[player_id] = new PlayerProgression
        {
            player_id = player_id,
            current_xp = 0,
            current_tier = "rookie",
            tier_progression = new Dictionary<string, TierData>
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
            }
        };
    }

    var state = new SeasonState { season_id = season_id, teams = teams };
    var ctrl = new SeasonController(state, player_progressions);
    active_seasons[season_id] = ctrl;

    LogHelper.LogEvent("SeasonStart", new { timestamp = DateTime.UtcNow.ToString("o"), season_id = season_id, team_count = teams.Count });

    var json = JsonSerializer.Serialize(ctrl.state, jsonOptions);
    File.WriteAllText($"data/seasons/{season_id}.json", json);
    Console.WriteLine($"Saved file to: {Path.GetFullPath($"data/seasons/{season_id}.json")}");

    return Results.Json(ctrl.state);
});

app.MapPost("/seasons/{id}/simulate_week", (string id) =>
{
    if (!active_seasons.TryGetValue(id, out var ctrl))
        return Results.NotFound($"Season {id} not found.");

    // --- NEW: Add this Lock Block ---
    lock (ctrl)
    {
        // Optional: specific check to stop double-simulation if needed
        // if (ctrl.state.week >= target_week) return Results.Json(ctrl.state);

        ctrl.SimulateWeek();

        // Safe to write now because only one thread can be here at a time
        var json = JsonSerializer.Serialize(ctrl.state, jsonOptions);
        
        // We use the specific week number in the filename
        string filePath = $"data/seasons/{id}_week_{ctrl.state.week}.json";
        
        // Simple retry logic in case OneDrive is momentarily locking the file
        int retries = 3;
        while (retries > 0)
        {
            try 
            {
                File.WriteAllText(filePath, json);
                break; // Success
            }
            catch (IOException) 
            {
                retries--;
                System.Threading.Thread.Sleep(50); // Wait 50ms and try again
                if (retries == 0) throw; // If it still fails, crash
            }
        }
    }
    // --------------------------------

    return Results.Json(ctrl.state);
});

app.MapGet("/seasons/{id}", (string id) =>
{
    if (!active_seasons.TryGetValue(id, out var ctrl))
        return Results.NotFound($"Season {id} not found.");
    return Results.Json(ctrl.state);
});

app.MapGet("/progression/{player_id}", (string player_id) =>
{
    if (!player_progressions.TryGetValue(player_id, out var progression))
        return Results.NotFound($"Player {player_id} not found.");

    var json = JsonSerializer.Serialize(progression, jsonOptions);
    File.WriteAllText($"data/progression/{player_id}.json", json);

    return Results.Json(progression);
});

app.Run();

namespace FMGSeasonProgression
{
    public class TeamData
    {
        [JsonPropertyName("team_id")]
        public string team_id { get; set; } = "";

        [JsonPropertyName("player_id")]
        public string player_id { get; set; } = "";

        [JsonPropertyName("team_name")]
        public string team_name { get; set; } = "";

        [JsonPropertyName("rating")]
        public int rating { get; set; }

        [JsonPropertyName("is_player_team")]
        public bool is_player_team { get; set; }

        [JsonPropertyName("wins")]
        public int wins { get; set; } = 0;

        [JsonPropertyName("losses")]
        public int losses { get; set; } = 0;
    }

    public class SeasonState
    {
        [JsonPropertyName("season_id")]
        public string season_id { get; set; } = "";

        [JsonPropertyName("teams")]
        public List<TeamData> teams { get; set; } = new();

        [JsonPropertyName("week")]
        public int week { get; set; } = 0;
        
        [JsonPropertyName("player_progression")]
        public List<PlayerProgression> player_progression { get; set; } = new();
    }

    public class PlayerProgression
    {
        [JsonPropertyName("player_id")]
        public string player_id { get; set; } = "";

        [JsonPropertyName("current_xp")]
        public int current_xp { get; set; } = 0;

        [JsonPropertyName("current_tier")]
        public string current_tier { get; set; } = "rookie";

        [JsonPropertyName("tier_progression")]
        public Dictionary<string, TierData> tier_progression { get; set; } = new();

        [JsonPropertyName("xp_history")]
        public List<XPHistoryEntry> xp_history { get; set; } = new();
    }

    public class TierData
    {
        [JsonPropertyName("min_xp")]
        public int min_xp { get; set; }

        [JsonPropertyName("max_xp")]
        public int max_xp { get; set; }

        [JsonPropertyName("display_name")]
        public string display_name { get; set; } = "";

        [JsonPropertyName("unlock_features")]
        public List<string> unlock_features { get; set; } = new();
    }

    public class XPHistoryEntry
    {
        [JsonPropertyName("timestamp")]
        public string timestamp { get; set; } = DateTime.UtcNow.ToString("o");

        [JsonPropertyName("xp_gained")]
        public int xp_gained { get; set; }

        [JsonPropertyName("source")]
        public string source { get; set; } = "match_played";

        [JsonPropertyName("facility_multiplier")]
        public float facility_multiplier { get; set; } = 1.0f;

        [JsonPropertyName("coaching_bonus")]
        public float coaching_bonus { get; set; } = 1.0f;
    }

    public static class LogHelper
    {
        public static void LogEvent(string event_name, object details)
        {
            var json = JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = false });
            Console.WriteLine($"[{DateTime.Now}] {event_name}: {json}");
        }
    }

    public class SeasonController
    {
        public SeasonState state { get; private set; }
        private readonly ConcurrentDictionary<string, PlayerProgression> player_progressions;
        private readonly Random rnd = new();

        public SeasonController(SeasonState state, ConcurrentDictionary<string, PlayerProgression> progressions)
        {
            this.state = state;
            player_progressions = progressions;
        }

        public void SimulateWeek()
        {
            var teams = state.teams.OrderBy(t => rnd.Next()).ToList();

            for (int i = 0; i < teams.Count; i += 2)
            {
                if (i + 1 >= teams.Count) break;
                var team_a = teams[i];
                var team_b = teams[i + 1];

                double a_win_prob = 1.0 / (1.0 + Math.Pow(10, (team_b.rating - team_a.rating) / 400.0));
                bool a_wins = rnd.NextDouble() < a_win_prob;

                if (a_wins)
                {
                    team_a.wins++;
                    team_b.losses++;
                    team_a.rating += 10;
                    team_b.rating -= 10;
                    UpdateXP(team_a.player_id, 50, "match_win");
                    UpdateXP(team_b.player_id, 20, "match_loss");
                }
                else
                {
                    team_b.wins++;
                    team_a.losses++;
                    team_b.rating += 10;
                    team_a.rating -= 10;
                    UpdateXP(team_b.player_id, 50, "match_win");
                    UpdateXP(team_a.player_id, 20, "match_loss");
                }
            }

            state.week++;
            LogHelper.LogEvent("SimulateWeek", new { timestamp = DateTime.UtcNow.ToString("o"), week = state.week, teams = state.teams.Count });
        }

        private void UpdateXP(string player_id, int xp_gained, string source)
        {
            if (player_progressions.TryGetValue(player_id, out var progression))
            {
                progression.current_xp += xp_gained;

                foreach (var kvp in progression.tier_progression)
                {
                    var tierName = kvp.Key;
                    var tier = kvp.Value;

                    if (progression.current_xp >= tier.min_xp && progression.current_xp <= tier.max_xp)
                    {
                        progression.current_tier = tierName;
                        break;
                    }
                }

                progression.xp_history.Add(new XPHistoryEntry
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    xp_gained = xp_gained,
                    source = source,
                    facility_multiplier = 1.0f,
                    coaching_bonus = 1.0f
                });

                var json = JsonSerializer.Serialize(progression, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText($"data/progression/{player_id}.json", json);

                LogHelper.LogEvent("TierUpdate", new
                {
                    player_id,
                    new_tier = progression.current_tier,
                    current_xp = progression.current_xp
                });
            }
        }
    }

    public class CreateSeasonRequest
    {
        [JsonPropertyName("teams")]
        public List<Dictionary<string, object>> teams { get; set; } = new();

        [JsonPropertyName("player_team_name")]
        public string player_team_name { get; set; } = "";
    }
}
