using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TierData
{
    public int min_xp;
    public int max_xp;
    public string display_name;
    public List<string> unlock_features;
}

[System.Serializable]
public class TeamStatsSaveData
{
    public int wins;
    public int losses;
    public int points;
    public int total_matches;
}

[System.Serializable]
public class TeamProgressionSaveData
{
    public int total_xp;
    public int current_level;
    public string tier;
    public List<Dictionary<string, object>> xp_history;
}

[System.Serializable]
public class TeamSaveData
{
    public string team_id;
    public string player_id;
    public string team_name;
    public int rating;
    public bool is_player_team;
    public int rank;
    public TeamStatsSaveData stats;
    public TeamProgressionSaveData progression;
}

[System.Serializable]
public class PlayerProgressionSaveData
{
    public string player_id;
    public int current_xp;
    public string current_tier;
    public Dictionary<string, TierData> tier_progression;
    public List<XpHistoryEntry> xp_history;
}

[System.Serializable]
public class SeasonSaveData
{
    public string season_id;
    public int current_week;
    public int total_weeks;
    public List<TeamSaveData> teams;
    public List<PlayerProgressionSaveData> player_progression;
}

[System.Serializable]
public class Player
{
    public string player_id;
    public string display_name;
    public string created_at;
    public Player(string id, string name)
    {
        player_id = id;
        display_name = name;
        created_at = DateTime.UtcNow.ToString("o");
    }
}

[System.Serializable]
public class XpHistoryEntry
{
    public string id;
    public string player_id;
    public string timestamp;
    public int xp_gained;
    public string source;

    public XpHistoryEntry() {}

    
    public XpHistoryEntry(string playerId, int xpGained, string source)
    {
        id = Guid.NewGuid().ToString();
        player_id = playerId;
        timestamp = DateTime.UtcNow.ToString("o");
        xp_gained = xpGained;
        this.source = source;
    }


}

[System.Serializable]
public class PlayerProgressionState
{
    public string player_id;
    public int current_xp;
    public string current_tier;
    public List<XpHistoryEntry> xp_history;

    public PlayerProgressionState(string playerId)
    {
        player_id = playerId;
        current_xp = 0;
        current_tier = "rookie";
        xp_history = new List<XpHistoryEntry>();
    }
}
