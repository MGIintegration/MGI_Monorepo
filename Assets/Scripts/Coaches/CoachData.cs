
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Coach", menuName = "FMG/Coach Data")]
public class CoachData : ScriptableObject
{
    [Header("Basic Information")]
    public string coachName;
    public CoachType position;
    public Sprite coachPortrait;

    [Header("Contract Details")]
    [Range(1000, 50000)]
    public int weeklySalary = 5000;

    [Range(1, 5)]
    public int starRating = 3;

    [Header("Performance Bonuses")]
    [Range(0, 50)]
    public int offenseBonus = 0;

    [Range(0, 50)]
    public int defenseBonus = 0;

    [Range(0, 50)]
    public int specialTeamsBonus = 0;

    [Header("Coach Personality")]
    [TextArea(3, 5)]
    public string coachDescription;

    [Header("Database Integration - Extended Stats")]
    public string coach_id;
    public int experience = 1;
    public int championship_won = 0;
    public float overall_rating = 3.0f;
    public int contract_length = 1;
    public string current_team = "";
    public string prev_team = "";
    
    [Header("Detailed Performance Stats")]
    // Defensive Stats
    [Range(0, 10)] public float run_defence = 5.0f;
    [Range(0, 10)] public float pressure_control = 5.0f;
    [Range(0, 10)] public float coverage_discipline = 5.0f;
    [Range(0, 10)] public float turnover = 5.0f;
    
    // Offensive Stats  
    [Range(0, 10)] public float passing_efficiency = 5.0f;
    [Range(0, 10)] public float rush = 5.0f;
    [Range(0, 10)] public float red_zone_conversion = 5.0f;
    [Range(0, 10)] public float play_variation = 5.0f;
    
    // Special Teams Stats
    [Range(0, 10)] public float field_goal_accuracy = 5.0f;
    [Range(0, 10)] public float kickoff_instance = 5.0f;
    [Range(0, 10)] public float return_speed = 5.0f;
    [Range(0, 10)] public float return_coverage = 5.0f;

    [Header("Dynamic Data - Runtime Only")]
    [System.NonSerialized]
    public float currentPerformance = 1.0f;

    [System.NonSerialized]
    public int gamesCoached = 0;

    [System.NonSerialized]
    public bool isHired = false;

    // Static method to create CoachData from database record
    public static CoachData CreateFromDatabaseRecord(CoachDatabaseRecord dbRecord)
    {
        var coachData = CreateInstance<CoachData>();
        
        // Basic Information
        coachData.coachName = dbRecord.coach_name;
        coachData.position = ConvertStringToCoachType(dbRecord.coach_type);
        
        // Contract Details
        coachData.weeklySalary = Mathf.RoundToInt(dbRecord.salary);
        coachData.starRating = Mathf.RoundToInt(dbRecord.overall_rating);
        
        // Database Integration
        coachData.coach_id = dbRecord.coach_id;
        coachData.experience = dbRecord.experience;
        coachData.championship_won = dbRecord.championship_won;
        coachData.overall_rating = dbRecord.overall_rating;
        coachData.contract_length = dbRecord.contract_length;
        coachData.current_team = dbRecord.current_team;
        coachData.prev_team = dbRecord.prev_team;
        
        // Detailed Stats
        coachData.run_defence = dbRecord.run_defence;
        coachData.pressure_control = dbRecord.pressure_control;
        coachData.coverage_discipline = dbRecord.coverage_discipline;
        coachData.turnover = dbRecord.turnover;
        coachData.passing_efficiency = dbRecord.passing_efficiency;
        coachData.rush = dbRecord.rush;
        coachData.red_zone_conversion = dbRecord.red_zone_conversion;
        coachData.play_variation = dbRecord.play_variation;
        coachData.field_goal_accuracy = dbRecord.field_goal_accuracy;
        coachData.kickoff_instance = dbRecord.kickoff_instance;
        coachData.return_speed = dbRecord.return_speed;
        coachData.return_coverage = dbRecord.return_coverage;
        
        // Auto-calculate bonuses from detailed stats
        coachData.defenseBonus = Mathf.RoundToInt((dbRecord.run_defence + dbRecord.pressure_control + dbRecord.coverage_discipline + dbRecord.turnover) / 4.0f * 5.0f);
        coachData.offenseBonus = Mathf.RoundToInt((dbRecord.passing_efficiency + dbRecord.rush + dbRecord.red_zone_conversion + dbRecord.play_variation) / 4.0f * 5.0f);
        coachData.specialTeamsBonus = Mathf.RoundToInt((dbRecord.field_goal_accuracy + dbRecord.kickoff_instance + dbRecord.return_speed + dbRecord.return_coverage) / 4.0f * 5.0f);
        
        return coachData;
    }
    
    private static CoachType ConvertStringToCoachType(string typeString)
    {
        switch (typeString?.ToUpper())
        {
            case "D": return CoachType.Defense;
            case "O": return CoachType.Offense;
            case "S": return CoachType.SpecialTeams;
            default: return CoachType.Offense;
        }
    }

    public CoachType type;

    // Calculated properties
    public int TotalBonus => offenseBonus + defenseBonus + specialTeamsBonus;
    public bool IsSpecialist => TotalBonus > 0 && GetSpecialtyCount() == 1;

    private int GetSpecialtyCount()
    {
        int count = 0;
        if (offenseBonus > 0) count++;
        if (defenseBonus > 0) count++;
        if (specialTeamsBonus > 0) count++;
        return count;
    }

    // Validation
    private void OnValidate()
    {
        // Ensure coach has at least one specialty
        if (TotalBonus == 0)
        {
            Debug.LogWarning($"Coach {coachName} has no bonuses assigned!");
        }

        // Validate salary ranges
        if (weeklySalary < 1000)
            weeklySalary = 1000;
    }


    public int GetEffectiveDefenseBonus()
    {
        return Mathf.RoundToInt(defenseBonus * currentPerformance);
    }

    public int GetEffectiveOffenseBonus()
    {
        return Mathf.RoundToInt(offenseBonus * currentPerformance);
    }

    public int GetEffectiveSpecialBonus()
    {
        return Mathf.RoundToInt(specialTeamsBonus * currentPerformance);
    }

}

// Database Integration Structures
[System.Serializable]
public class CoachDatabaseRecord
{
    public string coach_id;
    public string coach_name;
    public string coach_type; // D, O, S
    public int experience;
    public int championship_won;
    public float overall_rating;
    public float salary;
    public int contract_length;
    public string current_team;
    public string prev_team;
    
    // Defensive Stats
    public float run_defence;
    public float pressure_control;
    public float coverage_discipline;
    public float turnover;
    
    // Offensive Stats  
    public float passing_efficiency;
    public float rush;
    public float red_zone_conversion;
    public float play_variation;
    
    // Special Teams Stats
    public float field_goal_accuracy;
    public float kickoff_instance;
    public float return_speed;
    public float return_coverage;
}

[System.Serializable]
public class SpecialtyEntry
{
    public string key;
    public int value;
}

[System.Serializable]
public class JsonWrapper
{
    public CoachDatabaseRecord[] Items;
}

public enum CoachType
{
    Offense,
    Defense,
    SpecialTeams
}


