using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class StatEntry
{
    public string stat;
    public Sprite icon;
    public int beforeValue; // Value before change
    public int afterValue;  // Value after change
}

[System.Serializable]
public class WeeklySummaryData
{
    public int weeklyInvestment;
    public float performanceGainPercent;
    public int playoffBonus;
}

[System.Serializable]
public class TeamDatabaseRecord
{
    public string team_id;
    public string team_name;
    public string league;
    public float budget;
    public float overall_rating;
    public float defence_rating;
    public float offence_rating;
    public float special_teams_rating;
    public string description;
}

[System.Serializable]
public class TeamJsonWrapper
{
    public TeamDatabaseRecord[] Items;
}