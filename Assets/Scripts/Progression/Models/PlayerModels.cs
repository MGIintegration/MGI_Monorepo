using System;
using System.Collections.Generic;
using UnityEngine;


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
