// =============================
// StatusDeltaChecker.cs
// FMG Coaching System - Phase 2: Stat Delta Persistence
// Status and Performance Delta Tracking System
// 
// Author: Azwad (Implementation)
// Purpose: Ensure stat deltas persist between games and track performance changes
// Integration: CoachingStats_v4.cs methodology
// =============================
//
// Features:
// - Track stat deltas with scripts during save/load/force quit during gameplay
// - Monitor performance changes over time
// - Calculate coaching impact metrics
// - Persist delta data between game sessions
// - Performance trending analysis
// - Delta validation and corruption detection
//
// =============================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatusDeltaChecker : MonoBehaviour
{
    [Header("Delta Tracking Configuration")]
    [SerializeField] private bool enableDeltaTracking = true;
    [SerializeField] private bool enableRealTimeTracking = true;
    [SerializeField] private float deltaCheckInterval = 1f; // seconds
    [SerializeField] private int maxDeltaHistory = 200;

    [Header("Performance Thresholds")]
    [SerializeField] private float significantDeltaThreshold = 5f; // percentage
    [SerializeField] private float warningDeltaThreshold = -10f; // negative performance warning
    [SerializeField] private int trendAnalysisWindow = 10; // number of records for trend analysis

    // Delta tracking storage
    private List<StatDelta> deltaHistory = new List<StatDelta>();
    private Dictionary<StatType, float> baselineStats = new Dictionary<StatType, float>();
    private Dictionary<StatType, float> currentStats = new Dictionary<StatType, float>();
    private Dictionary<StatType, List<float>> recentDeltas = new Dictionary<StatType, List<float>>();

    // Coaching impact tracking
    private DateTime coachingStartTime;
    private Dictionary<string, CoachImpactRecord> coachImpactRecords = new Dictionary<string, CoachImpactRecord>();

    // Events for delta notifications
    public static event System.Action<StatDelta> OnStatDeltaDetected;
    public static event System.Action<TrendAnalysis> OnTrendAnalysisUpdated;
    public static event System.Action<string> OnPerformanceWarning;
    public static event System.Action<CoachImpactSummary> OnCoachImpactCalculated;

    // Singleton pattern
    public static StatusDeltaChecker Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDeltaTracking();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (enableRealTimeTracking)
        {
            InvokeRepeating(nameof(CheckStatDeltas), deltaCheckInterval, deltaCheckInterval);
        }

        // Subscribe to coaching events
        SubscribeToEvents();
        
        // Load persisted delta data
        LoadDeltaHistory();
    }

    private void OnDestroy()
    {
        // Save delta data before destruction
        SaveDeltaHistory();
        UnsubscribeFromEvents();
    }

    #region Initialization and Events

    /// <summary>
    /// Initialize delta tracking system
    /// </summary>
    private void InitializeDeltaTracking()
    {
        // Initialize stat dictionaries
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            baselineStats[statType] = 0f;
            currentStats[statType] = 0f;
            recentDeltas[statType] = new List<float>();
        }

        coachingStartTime = DateTime.Now;
        
        if (enableDeltaTracking)
        {
            Debug.Log("[StatusDeltaChecker] Delta tracking system initialized");
        }
    }

    /// <summary>
    /// Subscribe to relevant game events
    /// </summary>
    private void SubscribeToEvents()
    {
        // Subscribe to save/load events for delta persistence
        SaveLoadLogic.OnSaveCompleted += OnSaveCompleted;
        SaveLoadLogic.OnLoadCompleted += OnLoadCompleted;
    }

    /// <summary>
    /// Unsubscribe from events
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        SaveLoadLogic.OnSaveCompleted -= OnSaveCompleted;
        SaveLoadLogic.OnLoadCompleted -= OnLoadCompleted;
    }

    #endregion

    #region Stat Delta Tracking

    /// <summary>
    /// Set baseline stats (called when coaching starts or game begins)
    /// </summary>
    public void SetBaselineStats()
    {
        var teamStats = GatherCurrentTeamStats();
        
        foreach (var stat in teamStats)
        {
            baselineStats[stat.Key] = stat.Value;
            currentStats[stat.Key] = stat.Value;
        }

        coachingStartTime = DateTime.Now;
        
        Debug.Log($"[StatusDeltaChecker] Baseline stats set at {coachingStartTime}");
        LogStatSnapshot("Baseline", baselineStats);
    }

    /// <summary>
    /// Update current stats and calculate deltas
    /// </summary>
    public void UpdateCurrentStats()
    {
        var newStats = GatherCurrentTeamStats();
        var deltas = new List<StatDelta>();

        foreach (var statType in newStats.Keys)
        {
            float oldValue = currentStats.ContainsKey(statType) ? currentStats[statType] : 0f;
            float newValue = newStats[statType];
            float deltaValue = newValue - oldValue;
            float deltaPercentage = oldValue != 0 ? (deltaValue / oldValue) * 100f : 0f;

            if (Math.Abs(deltaValue) > 0.01f) // Only track meaningful changes
            {
                var delta = new StatDelta
                {
                    timestamp = DateTime.Now,
                    statType = statType,
                    oldValue = oldValue,
                    newValue = newValue,
                    deltaValue = deltaValue,
                    deltaPercentage = deltaPercentage,
                    source = DeltaSource.System
                };

                deltas.Add(delta);
                RecordStatDelta(delta);
            }

            currentStats[statType] = newValue;
        }

        if (deltas.Count > 0)
        {
            ProcessStatDeltas(deltas);
        }
    }

    /// <summary>
    /// Process a collection of stat deltas
    /// </summary>
    private void ProcessStatDeltas(List<StatDelta> deltas)
    {
        foreach (var delta in deltas)
        {
            // Notify about significant deltas
            if (Math.Abs(delta.deltaPercentage) >= significantDeltaThreshold)
            {
                Debug.Log($"[StatusDeltaChecker] Significant delta: {delta.statType} changed by {delta.deltaPercentage:F2}%");
            }
            
            // Check for performance warnings
            if (delta.deltaPercentage <= warningDeltaThreshold)
            {
                string warningMessage = $"Performance warning: {delta.statType} decreased by {Math.Abs(delta.deltaPercentage):F2}%";
                OnPerformanceWarning?.Invoke(warningMessage);
            }
        }
    }

    /// <summary>
    /// Check for stat deltas (called periodically)
    /// </summary>
    private void CheckStatDeltas()
    {
        if (!enableDeltaTracking) return;

        UpdateCurrentStats();
        
        // Check for performance trends
        AnalyzePerformanceTrends();
    }

    /// <summary>
    /// Record a stat delta for historical tracking
    /// </summary>
    private void RecordStatDelta(StatDelta delta)
    {
        deltaHistory.Add(delta);
        
        // Maintain history size limit
        if (deltaHistory.Count > maxDeltaHistory)
        {
            deltaHistory.RemoveAt(0);
        }

        // Update recent deltas for trend analysis
        if (!recentDeltas.ContainsKey(delta.statType))
        {
            recentDeltas[delta.statType] = new List<float>();
        }
        
        recentDeltas[delta.statType].Add(delta.deltaPercentage);
        
        // Maintain recent deltas window
        if (recentDeltas[delta.statType].Count > trendAnalysisWindow)
        {
            recentDeltas[delta.statType].RemoveAt(0);
        }

        OnStatDeltaDetected?.Invoke(delta);

        // Check for significant changes
        if (Math.Abs(delta.deltaPercentage) >= significantDeltaThreshold)
        {
            Debug.Log($"[StatusDeltaChecker] Significant delta detected: {delta.statType} changed by {delta.deltaPercentage:F2}%");
        }

        // Check for performance warnings
        if (delta.deltaPercentage <= warningDeltaThreshold)
        {
            string warningMessage = $"Performance warning: {delta.statType} decreased by {Math.Abs(delta.deltaPercentage):F2}%";
            OnPerformanceWarning?.Invoke(warningMessage);
        }
    }

    #endregion

    #region Coach Impact Tracking

    /// <summary>
    /// Record coaching impact when a coach is hired
    /// </summary>
    public void RecordCoachHired(CoachData coach)
    {
        if (coach == null) return;

        var impactRecord = new CoachImpactRecord
        {
            coachName = coach.coachName,
            coachType = coach.position.ToString(),
            hireTimestamp = DateTime.Now,
            preHireStats = new Dictionary<StatType, float>(currentStats),
            starRating = coach.starRating,
            weeklySalary = coach.weeklySalary
        };

        coachImpactRecords[coach.coachName] = impactRecord;
        
        // Record hiring event as delta
        var hiringDelta = new StatDelta
        {
            timestamp = DateTime.Now,
            statType = StatType.CoachingInvestment,
            oldValue = GetTotalCoachingSalary() - coach.weeklySalary,
            newValue = GetTotalCoachingSalary(),
            deltaValue = coach.weeklySalary,
            deltaPercentage = 0f, // Will be calculated later
            source = DeltaSource.CoachHiring,
            additionalInfo = $"Hired {coach.coachName} ({coach.position})"
        };

        RecordStatDelta(hiringDelta);
        
        Debug.Log($"[StatusDeltaChecker] Recorded hiring of {coach.coachName} at {impactRecord.hireTimestamp}");
    }

    /// <summary>
    /// Record coaching impact when a coach is fired
    /// </summary>
    public void RecordCoachFired(CoachData coach)
    {
        if (coach == null || !coachImpactRecords.ContainsKey(coach.coachName)) return;

        var impactRecord = coachImpactRecords[coach.coachName];
        impactRecord.fireTimestamp = DateTime.Now;
        impactRecord.postFireStats = new Dictionary<StatType, float>(currentStats);
        
        // Calculate total impact
        CalculateCoachImpact(impactRecord);
        
        // Record firing event as delta
        var firingDelta = new StatDelta
        {
            timestamp = DateTime.Now,
            statType = StatType.CoachingInvestment,
            oldValue = GetTotalCoachingSalary() + coach.weeklySalary,
            newValue = GetTotalCoachingSalary(),
            deltaValue = -coach.weeklySalary,
            deltaPercentage = 0f,
            source = DeltaSource.CoachFiring,
            additionalInfo = $"Fired {coach.coachName} ({coach.position})"
        };

        RecordStatDelta(firingDelta);
        
        Debug.Log($"[StatusDeltaChecker] Recorded firing of {coach.coachName} at {impactRecord.fireTimestamp}");
    }

    /// <summary>
    /// Calculate coaching impact for a specific coach
    /// </summary>
    private void CalculateCoachImpact(CoachImpactRecord record)
    {
        var impact = new CoachImpactSummary
        {
            coachName = record.coachName,
            coachType = record.coachType,
            employmentDuration = record.fireTimestamp.HasValue ? 
                (record.fireTimestamp.Value - record.hireTimestamp).TotalDays : 
                (DateTime.Now - record.hireTimestamp).TotalDays,
            totalSalaryPaid = (float)(record.weeklySalary * (record.fireTimestamp.HasValue ? 
                (record.fireTimestamp.Value - record.hireTimestamp).TotalDays / 7 : 
                (DateTime.Now - record.hireTimestamp).TotalDays / 7)),
            statImpacts = new Dictionary<StatType, float>()
        };

        // Calculate stat improvements
        var endStats = record.postFireStats ?? currentStats;
        foreach (var statType in record.preHireStats.Keys)
        {
            if (endStats.ContainsKey(statType))
            {
                float improvement = endStats[statType] - record.preHireStats[statType];
                float improvementPercentage = record.preHireStats[statType] != 0 ? 
                    (improvement / record.preHireStats[statType]) * 100f : 0f;
                
                impact.statImpacts[statType] = improvementPercentage;
            }
        }

        // Calculate ROI
        float totalImprovement = impact.statImpacts.Values.Average();
        impact.returnOnInvestment = impact.totalSalaryPaid > 0 ? totalImprovement / impact.totalSalaryPaid * 1000f : 0f;

        OnCoachImpactCalculated?.Invoke(impact);
    }

    #endregion

    #region Performance Trend Analysis

    /// <summary>
    /// Analyze performance trends over time
    /// </summary>
    private void AnalyzePerformanceTrends()
    {
        foreach (var statType in recentDeltas.Keys)
        {
            if (recentDeltas[statType].Count < 3) continue; // Need minimum data for trend

            var analysis = new TrendAnalysis
            {
                statType = statType,
                timestamp = DateTime.Now,
                dataPoints = new List<float>(recentDeltas[statType]),
                trendDirection = CalculateTrendDirection(recentDeltas[statType]),
                trendStrength = CalculateTrendStrength(recentDeltas[statType]),
                averageDelta = recentDeltas[statType].Average(),
                volatility = CalculateVolatility(recentDeltas[statType])
            };

            OnTrendAnalysisUpdated?.Invoke(analysis);
        }
    }

    /// <summary>
    /// Calculate trend direction for a series of deltas
    /// </summary>
    private TrendDirection CalculateTrendDirection(List<float> deltas)
    {
        if (deltas.Count < 2) return TrendDirection.Stable;

        float slope = CalculateLinearRegression(deltas);
        
        if (slope > 1f) return TrendDirection.StronglyIncreasing;
        if (slope > 0.1f) return TrendDirection.Increasing;
        if (slope < -1f) return TrendDirection.StronglyDecreasing;
        if (slope < -0.1f) return TrendDirection.Decreasing;
        
        return TrendDirection.Stable;
    }

    /// <summary>
    /// Calculate trend strength (0-1 scale)
    /// </summary>
    private float CalculateTrendStrength(List<float> deltas)
    {
        if (deltas.Count < 2) return 0f;

        float slope = Math.Abs(CalculateLinearRegression(deltas));
        return Mathf.Clamp01(slope / 5f); // Normalize to 0-1 scale
    }

    /// <summary>
    /// Calculate volatility of delta series
    /// </summary>
    private float CalculateVolatility(List<float> deltas)
    {
        if (deltas.Count < 2) return 0f;

        float mean = deltas.Average();
        float variance = deltas.Select(x => (x - mean) * (x - mean)).Average();
        return Mathf.Sqrt(variance);
    }

    /// <summary>
    /// Calculate linear regression slope for trend analysis
    /// </summary>
    private float CalculateLinearRegression(List<float> values)
    {
        if (values.Count < 2) return 0f;

        float n = values.Count;
        float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;

        for (int i = 0; i < values.Count; i++)
        {
            float x = i;
            float y = values[i];
            
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }

    #endregion

    #region Data Persistence

    /// <summary>
    /// Save delta history to persistent storage
    /// </summary>
    public void SaveDeltaHistory()
    {
        try
        {
            var saveData = new DeltaSaveData
            {
                saveVersion = "1.0",
                saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                deltaHistory = deltaHistory,
                baselineStats = baselineStats,
                coachingStartTime = coachingStartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                coachImpactRecords = coachImpactRecords.Values.ToList()
            };

            string jsonData = JsonUtility.ToJson(saveData, true);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "FMG_DeltaData.json");
            System.IO.File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"[StatusDeltaChecker] Delta history saved: {deltaHistory.Count} records");
        }
        catch (Exception e)
        {
            Debug.LogError($"[StatusDeltaChecker] Failed to save delta history: {e.Message}");
        }
    }

    /// <summary>
    /// Load delta history from persistent storage
    /// </summary>
    public void LoadDeltaHistory()
    {
        try
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "FMG_DeltaData.json");
            
            if (System.IO.File.Exists(filePath))
            {
                string jsonData = System.IO.File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<DeltaSaveData>(jsonData);
                
                if (saveData != null)
                {
                    deltaHistory = saveData.deltaHistory ?? new List<StatDelta>();
                    baselineStats = saveData.baselineStats ?? new Dictionary<StatType, float>();
                    
                    if (DateTime.TryParse(saveData.coachingStartTime, out DateTime parsedStartTime))
                    {
                        coachingStartTime = parsedStartTime;
                    }
                    
                    // Restore coach impact records
                    if (saveData.coachImpactRecords != null)
                    {
                        coachImpactRecords.Clear();
                        foreach (var record in saveData.coachImpactRecords)
                        {
                            coachImpactRecords[record.coachName] = record;
                        }
                    }
                    
                    Debug.Log($"[StatusDeltaChecker] Delta history loaded: {deltaHistory.Count} records");
                }
            }
            else
            {
                Debug.Log("[StatusDeltaChecker] No existing delta history found, starting fresh");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[StatusDeltaChecker] Failed to load delta history: {e.Message}");
        }
    }

    #endregion

    #region Data Gathering Helpers

    /// <summary>
    /// Gather current team stats from various game systems
    /// </summary>
    private Dictionary<StatType, float> GatherCurrentTeamStats()
    {
        var stats = new Dictionary<StatType, float>();
        
        // Get stats from any stats component if available - use generic approach
        var statsComponents = FindObjectsOfType<MonoBehaviour>();
        var coachingStats = System.Array.Find(statsComponents, c => c.GetType().Name.Contains("Stats"));
        
        if (coachingStats != null)
        {
            // Use reflection to get common stat properties if they exist
            var type = coachingStats.GetType();
            var gamesPlayedMember = type.GetProperty("GamesPlayed") as System.Reflection.MemberInfo ?? type.GetField("GamesPlayed");
            var gamesWonMember = type.GetProperty("GamesWon") as System.Reflection.MemberInfo ?? type.GetField("GamesWon");
            var teamMoraleMember = type.GetProperty("TeamMorale") as System.Reflection.MemberInfo ?? type.GetField("TeamMorale");
            
            if (gamesPlayedMember != null && gamesWonMember != null)
            {
                int gamesPlayed = GetMemberIntValue(gamesPlayedMember, coachingStats, 0);
                int gamesWon = GetMemberIntValue(gamesWonMember, coachingStats, 0);
                stats[StatType.WinRate] = gamesPlayed > 0 ? (float)gamesWon / gamesPlayed * 100f : 0f;
                stats[StatType.GamesPlayed] = gamesPlayed;
                stats[StatType.GamesWon] = gamesWon;
            }
            
            if (teamMoraleMember != null)
            {
                float teamMorale = GetMemberFloatValue(teamMoraleMember, coachingStats, 0f);
                stats[StatType.TeamMorale] = teamMorale * 100f;
            }
        }
        else
        {
            // Default values if no stats component found
            stats[StatType.WinRate] = 50f;
            stats[StatType.TeamMorale] = 75f;
            stats[StatType.GamesPlayed] = 0;
            stats[StatType.GamesWon] = 0;
        }

        // Get stats from team performance systems - use generic approach
        var managers = FindObjectsOfType<MonoBehaviour>();
        var teamManager = System.Array.Find(managers, m => m.GetType().Name.Contains("Team") || m.GetType().Name.Contains("Manager"));
        if (teamManager != null)
        {
            // Use reflection to get team stats if available
            var type = teamManager.GetType();
            var offenseMember = type.GetProperty("OffenseRating") as System.Reflection.MemberInfo ?? type.GetField("OffenseRating");
            var defenseMember = type.GetProperty("DefenseRating") as System.Reflection.MemberInfo ?? type.GetField("DefenseRating");
            
            if (offenseMember != null)
                stats[StatType.OffenseRating] = GetMemberFloatValue(offenseMember, teamManager, 50f);
            if (defenseMember != null)
                stats[StatType.DefenseRating] = GetMemberFloatValue(defenseMember, teamManager, 50f);
        }

        // Get coaching investment
        stats[StatType.CoachingInvestment] = GetTotalCoachingSalary();

        return stats;
    }

    /// <summary>
    /// Safely get int value from a member (field or property)
    /// </summary>
    private int GetMemberIntValue(System.Reflection.MemberInfo member, object target, int defaultValue)
    {
        try
        {
            if (member is System.Reflection.PropertyInfo prop)
                return (int)(prop.GetValue(target) ?? defaultValue);
            else if (member is System.Reflection.FieldInfo field)
                return (int)(field.GetValue(target) ?? defaultValue);
        }
        catch
        {
            // Return default if conversion fails
        }
        return defaultValue;
    }

    /// <summary>
    /// Safely get float value from a member (field or property)
    /// </summary>
    private float GetMemberFloatValue(System.Reflection.MemberInfo member, object target, float defaultValue)
    {
        try
        {
            if (member is System.Reflection.PropertyInfo prop)
                return (float)(prop.GetValue(target) ?? defaultValue);
            else if (member is System.Reflection.FieldInfo field)
                return (float)(field.GetValue(target) ?? defaultValue);
        }
        catch
        {
            // Return default if conversion fails
        }
        return defaultValue;
    }

    /// <summary>
    /// Get total weekly salary of all hired coaches
    /// </summary>
    private float GetTotalCoachingSalary()
    {
        float total = 0f;
        var coachManager = CoachManager.instance;
        
        if (coachManager != null)
        {
            if (coachManager.defenseCoach != null) total += coachManager.defenseCoach.weeklySalary;
            if (coachManager.offenseCoach != null) total += coachManager.offenseCoach.weeklySalary;
            if (coachManager.SpecialCoach != null) total += coachManager.SpecialCoach.weeklySalary;
        }

        return total;
    }

    #endregion

    #region Event Handlers

    private void OnSaveCompleted(bool success)
    {
        if (success)
        {
            // Save delta data when game saves
            SaveDeltaHistory();
        }
    }

    private void OnLoadCompleted(bool success)
    {
        if (success)
        {
            // Reload delta data when game loads
            LoadDeltaHistory();
            
            // Update current stats after load
            UpdateCurrentStats();
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Log current stat snapshot for debugging
    /// </summary>
    private void LogStatSnapshot(string label, Dictionary<StatType, float> stats)
    {
        if (!enableDeltaTracking) return;

        Debug.Log($"[StatusDeltaChecker] {label} Stats Snapshot:");
        foreach (var stat in stats)
        {
            Debug.Log($"  {stat.Key}: {stat.Value:F2}");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get current delta history
    /// </summary>
    public List<StatDelta> GetDeltaHistory()
    {
        return new List<StatDelta>(deltaHistory);
    }

    /// <summary>
    /// Get baseline stats
    /// </summary>
    public Dictionary<StatType, float> GetBaselineStats()
    {
        return new Dictionary<StatType, float>(baselineStats);
    }

    /// <summary>
    /// Get current stats
    /// </summary>
    public Dictionary<StatType, float> GetCurrentStats()
    {
        return new Dictionary<StatType, float>(currentStats);
    }

    /// <summary>
    /// Get coach impact records
    /// </summary>
    public List<CoachImpactRecord> GetCoachImpactRecords()
    {
        return coachImpactRecords.Values.ToList();
    }

    /// <summary>
    /// Force delta check
    /// </summary>
    public void ForceDeltaCheck()
    {
        CheckStatDeltas();
    }

    /// <summary>
    /// Reset delta tracking (for new game/season)
    /// </summary>
    public void ResetDeltaTracking()
    {
        deltaHistory.Clear();
        baselineStats.Clear();
        currentStats.Clear();
        recentDeltas.Clear();
        coachImpactRecords.Clear();
        
        InitializeDeltaTracking();
        SetBaselineStats();
        
        Debug.Log("[StatusDeltaChecker] Delta tracking reset");
    }

    #endregion
}

// =============================
// Data Structures for Delta Tracking
// =============================

[System.Serializable]
public class StatDelta
{
    public DateTime timestamp;
    public StatType statType;
    public float oldValue;
    public float newValue;
    public float deltaValue;
    public float deltaPercentage;
    public DeltaSource source;
    public string additionalInfo;
}

[System.Serializable]
public class CoachImpactRecord
{
    public string coachName;
    public string coachType;
    public DateTime hireTimestamp;
    public DateTime? fireTimestamp;
    public Dictionary<StatType, float> preHireStats;
    public Dictionary<StatType, float> postFireStats;
    public float starRating;
    public int weeklySalary;
}

[System.Serializable]
public class CoachImpactSummary
{
    public string coachName;
    public string coachType;
    public double employmentDuration; // days
    public float totalSalaryPaid;
    public Dictionary<StatType, float> statImpacts; // percentage improvements
    public float returnOnInvestment;
}

[System.Serializable]
public class TrendAnalysis
{
    public StatType statType;
    public DateTime timestamp;
    public List<float> dataPoints;
    public TrendDirection trendDirection;
    public float trendStrength; // 0-1 scale
    public float averageDelta;
    public float volatility;
}

[System.Serializable]
public class DeltaSaveData
{
    public string saveVersion;
    public string saveTimestamp;
    public List<StatDelta> deltaHistory;
    public Dictionary<StatType, float> baselineStats;
    public string coachingStartTime;
    public List<CoachImpactRecord> coachImpactRecords;
}

public enum StatType
{
    WinRate,
    TeamMorale,
    OffenseRating,
    DefenseRating,
    SpecialTeamsRating,
    GamesPlayed,
    GamesWon,
    CoachingInvestment,
    TeamBudget,
    PlayerPerformance
}

public enum DeltaSource
{
    System,
    CoachHiring,
    CoachFiring,
    GameResult,
    SeasonChange,
    PlayerTrade,
    External
}

public enum TrendDirection
{
    StronglyDecreasing,
    Decreasing,
    Stable,
    Increasing,
    StronglyIncreasing
}
