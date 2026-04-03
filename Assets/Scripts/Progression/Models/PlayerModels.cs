using System;
using System.Collections.Generic;

namespace MGI.Progression.Models
{
    [Serializable]
    public class Player
    {
        public string player_id;
        public string display_name;
        public DateTime created_at;
    }

    [Serializable]
    public class PlayerProgressionState
    {
        public string player_id;
        public int current_xp;
        public int current_tier;
        public List<XpHistoryEntry> xp_history = new List<XpHistoryEntry>();
    }

    [Serializable]
    public class XpHistoryEntry
    {
        public string id;
        public string player_id;
        public DateTime timestamp;
        public int xp_gained;
        public string source;
    }
}
