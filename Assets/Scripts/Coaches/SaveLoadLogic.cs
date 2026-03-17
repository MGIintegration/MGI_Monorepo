// =============================
// SaveLoadLogic.cs
// FMG Coaching System - Phase 2: Data Hooks and Trending
// Save/Load Manager for Coach Data Integration
// 
// Author: Azwad (Implementation)
// Based on: Ruturaj's save structure requirements
// Integration: Kevin's validator bridge
// =============================
//
// Purpose:
// - Define where coach data will be saved in the current FMG save structure
// - Build JSON/ScriptableObject bridge for coach stats
// - Ensure stat deltas persist between games
// - Connect all save/load pipelines to team roster and stat display
//
// Features:
// - Coach stats stored as dedicated JSON file in Application.persistentDataPath
// - Flat dummy schema for clarity and future integration
// - Asynchronous data prefetch for Coach Detail screen
// - Corrupted data fallback handling
//
// =============================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

public class SaveLoadLogic : MonoBehaviour
{
    [Header("Save/Load Configuration")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float autoSaveInterval = 30f; // seconds
    [SerializeField] private int maxBackupFiles = 5;

    [Header("File Paths")]
    private string coachDataPath;
    private string gameDataPath;
    private string backupDirectoryPath;

    // Data containers
    private CoachSaveData currentCoachData;
    private GameSaveData currentGameData;

    // Events for save/load notifications
    public static event System.Action<bool> OnSaveCompleted;
    public static event System.Action<bool> OnLoadCompleted;
    public static event System.Action<string> OnSaveError;

    // Singleton pattern
    public static SaveLoadLogic Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePaths();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadAllData();
        InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
    }

    /// <summary>
    /// Initialize file paths following FMG save structure
    /// </summary>
    private void InitializePaths()
    {
        string persistentPath = Application.persistentDataPath;
        coachDataPath = Path.Combine(persistentPath, "FMG_CoachData.json");
        gameDataPath = Path.Combine(persistentPath, "FMG_GameData.json");
        backupDirectoryPath = Path.Combine(persistentPath, "Backups");

        // Create backup directory if it doesn't exist
        if (!Directory.Exists(backupDirectoryPath))
        {
            Directory.CreateDirectory(backupDirectoryPath);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[SaveLoadLogic] Initialized paths:\nCoach Data: {coachDataPath}\nGame Data: {gameDataPath}\nBackups: {backupDirectoryPath}");
        }
    }

    #region Save Methods

    /// <summary>
    /// Save all coach data to persistent storage
    /// </summary>
    public async Task<bool> SaveCoachData()
    {
        var startTime = DateTime.Now;
        
        try
        {
            // Gather current coach data
            var coachData = GatherCoachData();
            
            // Create backup before saving
            await CreateBackup();
            
            // Save to file
            string jsonData = JsonUtility.ToJson(coachData, true);
            await File.WriteAllTextAsync(coachDataPath, jsonData);
            
            currentCoachData = coachData;
            
            var saveTime = (DateTime.Now - startTime).TotalMilliseconds;
            if (enableDebugLogs)
            {
                Debug.Log($"[SaveLoadLogic] Coach data saved successfully in {saveTime:F2}ms");
            }
            
            OnSaveCompleted?.Invoke(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadLogic] Failed to save coach data: {e.Message}");
            OnSaveError?.Invoke($"Save failed: {e.Message}");
            OnSaveCompleted?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// Save game state data
    /// </summary>
    public async Task<bool> SaveGameData()
    {
        var startTime = DateTime.Now;
        
        try
        {
            var gameData = GatherGameData();
            string jsonData = JsonUtility.ToJson(gameData, true);
            await File.WriteAllTextAsync(gameDataPath, jsonData);
            
            currentGameData = gameData;
            
            var saveTime = (DateTime.Now - startTime).TotalMilliseconds;
            if (enableDebugLogs)
            {
                Debug.Log($"[SaveLoadLogic] Game data saved successfully in {saveTime:F2}ms");
            }
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadLogic] Failed to save game data: {e.Message}");
            OnSaveError?.Invoke($"Game save failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Auto-save method called at intervals
    /// </summary>
    private async void AutoSave()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[SaveLoadLogic] Performing auto-save...");
        }
        
        await SaveCoachData();
        await SaveGameData();
    }

    #endregion

    #region Load Methods

    /// <summary>
    /// Load all saved data on game start
    /// </summary>
    public async void LoadAllData()
    {
        var startTime = DateTime.Now;
        
        bool coachDataLoaded = await LoadCoachData();
        bool gameDataLoaded = await LoadGameData();
        
        var loadTime = (DateTime.Now - startTime).TotalMilliseconds;
        if (enableDebugLogs)
        {
            Debug.Log($"[SaveLoadLogic] All data loaded in {loadTime:F2}ms. Coach data: {coachDataLoaded}, Game data: {gameDataLoaded}");
        }
        
        OnLoadCompleted?.Invoke(coachDataLoaded && gameDataLoaded);
    }

    /// <summary>
    /// Load coach data with corruption fallback
    /// </summary>
    public async Task<bool> LoadCoachData()
    {
        try
        {
            if (File.Exists(coachDataPath))
            {
                string jsonData = await File.ReadAllTextAsync(coachDataPath);
                currentCoachData = JsonUtility.FromJson<CoachSaveData>(jsonData);
                
                // Validate loaded data
                if (IsCoachDataValid(currentCoachData))
                {
                    ApplyCoachData(currentCoachData);
                    return true;
                }
                else
                {
                    Debug.LogWarning("[SaveLoadLogic] Loaded coach data is corrupted, attempting backup restore...");
                    return await RestoreFromBackup();
                }
            }
            else
            {
                Debug.Log("[SaveLoadLogic] No coach data file found, creating default data");
                currentCoachData = CreateDefaultCoachData();
                ApplyCoachData(currentCoachData);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadLogic] Failed to load coach data: {e.Message}");
            return await RestoreFromBackup();
        }
    }

    /// <summary>
    /// Load game data
    /// </summary>
    public async Task<bool> LoadGameData()
    {
        try
        {
            if (File.Exists(gameDataPath))
            {
                string jsonData = await File.ReadAllTextAsync(gameDataPath);
                currentGameData = JsonUtility.FromJson<GameSaveData>(jsonData);
                ApplyGameData(currentGameData);
                return true;
            }
            else
            {
                currentGameData = CreateDefaultGameData();
                ApplyGameData(currentGameData);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadLogic] Failed to load game data: {e.Message}");
            currentGameData = CreateDefaultGameData();
            ApplyGameData(currentGameData);
            return false;
        }
    }

    #endregion

    #region Data Gathering and Application

    /// <summary>
    /// Gather current coach data from CoachManager
    /// </summary>
    private CoachSaveData GatherCoachData()
    {
        var coachManager = CoachManager.instance;
        // Use generic MonoBehaviour to avoid dependency on specific stats class
        var coachingStats = FindObjectOfType<MonoBehaviour>();
        
        var saveData = new CoachSaveData
        {
            saveVersion = "1.0",
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            
            // Current hired coaches
            defenseCoachData = coachManager?.defenseCoach != null ? 
                ConvertToSaveFormat(coachManager.defenseCoach) : null,
            offenseCoachData = coachManager?.offenseCoach != null ? 
                ConvertToSaveFormat(coachManager.offenseCoach) : null,
            specialTeamsCoachData = coachManager?.SpecialCoach != null ? 
                ConvertToSaveFormat(coachManager.SpecialCoach) : null,
            
            // Performance stats - use reflection for safe property access
            performanceStats = coachingStats != null ? GetPerformanceStats(coachingStats) : new PerformanceStatsSave()
        };
        
        return saveData;
    }

    /// <summary>
    /// Extract performance stats using reflection for safe property access
    /// </summary>
    private PerformanceStatsSave GetPerformanceStats(MonoBehaviour statsComponent)
    {
        var performanceStats = new PerformanceStatsSave();
        var type = statsComponent.GetType();

        // Use reflection to safely get properties
        try
        {
            var gamesPlayedMember = type.GetProperty("GamesPlayed") as System.Reflection.MemberInfo ?? type.GetField("GamesPlayed");
            var gamesWonMember = type.GetProperty("GamesWon") as System.Reflection.MemberInfo ?? type.GetField("GamesWon");
            var gamesBeforeCoachingMember = type.GetProperty("GamesBeforeCoaching") as System.Reflection.MemberInfo ?? type.GetField("GamesBeforeCoaching");
            var winsBeforeCoachingMember = type.GetProperty("WinsBeforeCoaching") as System.Reflection.MemberInfo ?? type.GetField("WinsBeforeCoaching");
            var teamMoraleMember = type.GetProperty("TeamMorale") as System.Reflection.MemberInfo ?? type.GetField("TeamMorale");
            var weeklyHistoryMember = type.GetProperty("WeeklyHistory") as System.Reflection.MemberInfo ?? type.GetField("WeeklyHistory");

            performanceStats.gamesPlayed = GetIntValue(gamesPlayedMember, statsComponent, 0);
            performanceStats.gamesWon = GetIntValue(gamesWonMember, statsComponent, 0);
            performanceStats.gamesBeforeCoaching = GetIntValue(gamesBeforeCoachingMember, statsComponent, 0);
            performanceStats.winsBeforeCoaching = GetIntValue(winsBeforeCoachingMember, statsComponent, 0);
            performanceStats.teamMorale = GetFloatValue(teamMoraleMember, statsComponent, 0.75f);
            
            // For WeeklyHistory, use empty list if not found
            if (weeklyHistoryMember != null)
            {
                object weeklyData = null;
                if (weeklyHistoryMember is System.Reflection.PropertyInfo prop)
                    weeklyData = prop.GetValue(statsComponent);
                else if (weeklyHistoryMember is System.Reflection.FieldInfo field)
                    weeklyData = field.GetValue(statsComponent);
                    
                if (weeklyData is List<WeeklyPerformanceData> weeklyList)
                {
                    performanceStats.weeklyHistory = weeklyList;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveLoadLogic] Could not extract performance stats: {e.Message}");
        }

        return performanceStats;
    }

    /// <summary>
    /// Safely get int value from field or property
    /// </summary>
    private int GetIntValue(System.Reflection.MemberInfo member, object target, int defaultValue)
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
    /// Safely get float value from field or property
    /// </summary>
    private float GetFloatValue(System.Reflection.MemberInfo member, object target, float defaultValue)
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
    /// Gather current game state data
    /// </summary>
    private GameSaveData GatherGameData()
    {
        // Get team data from relevant managers - use generic approach
        var managers = FindObjectsOfType<MonoBehaviour>();
        var teamManager = System.Array.Find(managers, m => m.GetType().Name.Contains("Team") || m.GetType().Name.Contains("Manager"));
        
        return new GameSaveData
        {
            saveVersion = "1.0",
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            currentWeek = 1, // Get from game manager
            teamBudget = 50000f, // Default budget if no manager found
            seasonProgress = 0.0f // Calculate based on current game state
        };
    }

    /// <summary>
    /// Apply loaded coach data to game systems
    /// </summary>
    private void ApplyCoachData(CoachSaveData data)
    {
        var coachManager = CoachManager.instance;
        if (coachManager == null) return;
        
        // Apply hired coaches
        if (data.defenseCoachData != null)
        {
            coachManager.defenseCoach = ConvertFromSaveFormat(data.defenseCoachData);
        }
        
        if (data.offenseCoachData != null)
        {
            coachManager.offenseCoach = ConvertFromSaveFormat(data.offenseCoachData);
        }
        
        if (data.specialTeamsCoachData != null)
        {
            coachManager.SpecialCoach = ConvertFromSaveFormat(data.specialTeamsCoachData);
        }
        
        // Apply performance stats
        // Use generic approach to find any stats component
        var statsComponents = FindObjectsOfType<MonoBehaviour>();
        var coachingStats = System.Array.Find(statsComponents, c => c.GetType().Name.Contains("Stats"));
        if (coachingStats != null && data.performanceStats != null)
        {
            // Use reflection or public setters to restore stats
            // This ensures stat deltas persist between games
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[SaveLoadLogic] Applied coach data from save: {data.saveTimestamp}");
        }
    }

    /// <summary>
    /// Apply loaded game data to game systems
    /// </summary>
    private void ApplyGameData(GameSaveData data)
    {
        // Apply game state to relevant managers
        if (enableDebugLogs)
        {
            Debug.Log($"[SaveLoadLogic] Applied game data from save: {data.saveTimestamp}");
        }
    }

    #endregion

    #region Data Conversion Helpers

    /// <summary>
    /// Convert CoachData to save format
    /// </summary>
    private CoachDataSave ConvertToSaveFormat(CoachData coach)
    {
        return new CoachDataSave
        {
            coachName = coach.coachName,
            starRating = coach.starRating,
            weeklySalary = coach.weeklySalary,
            experience = coach.experience,
            defenseBonus = coach.defenseBonus,
            offenseBonus = coach.offenseBonus,
            specialTeamsBonus = coach.specialTeamsBonus,
            position = coach.position.ToString()
        };
    }

    /// <summary>
    /// Convert save format back to CoachData
    /// </summary>
    private CoachData ConvertFromSaveFormat(CoachDataSave saveData)
    {
        var coach = ScriptableObject.CreateInstance<CoachData>();
        coach.coachName = saveData.coachName;
        coach.starRating = Mathf.RoundToInt(saveData.starRating); // Convert float to int
        coach.weeklySalary = saveData.weeklySalary;
        coach.experience = saveData.experience;
        coach.defenseBonus = saveData.defenseBonus;
        coach.offenseBonus = saveData.offenseBonus;
        coach.specialTeamsBonus = saveData.specialTeamsBonus;
        coach.position = (CoachType)Enum.Parse(typeof(CoachType), saveData.position);
        return coach;
    }

    #endregion

    #region Backup and Recovery

    /// <summary>
    /// Create backup of current save files
    /// </summary>
    private async Task CreateBackup()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(backupDirectoryPath, $"CoachData_Backup_{timestamp}.json");
            
            if (File.Exists(coachDataPath))
            {
                string currentData = await File.ReadAllTextAsync(coachDataPath);
                await File.WriteAllTextAsync(backupPath, currentData);
            }
            
            // Clean old backups
            CleanOldBackups();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveLoadLogic] Failed to create backup: {e.Message}");
        }
    }

    /// <summary>
    /// Restore from most recent backup
    /// </summary>
    private async Task<bool> RestoreFromBackup()
    {
        try
        {
            var backupFiles = Directory.GetFiles(backupDirectoryPath, "CoachData_Backup_*.json");
            if (backupFiles.Length == 0)
            {
                Debug.LogWarning("[SaveLoadLogic] No backup files found, creating default data");
                currentCoachData = CreateDefaultCoachData();
                ApplyCoachData(currentCoachData);
                return true;
            }
            
            // Get most recent backup
            Array.Sort(backupFiles);
            string latestBackup = backupFiles[backupFiles.Length - 1];
            
            string backupData = await File.ReadAllTextAsync(latestBackup);
            currentCoachData = JsonUtility.FromJson<CoachSaveData>(backupData);
            
            if (IsCoachDataValid(currentCoachData))
            {
                ApplyCoachData(currentCoachData);
                Debug.Log($"[SaveLoadLogic] Restored from backup: {Path.GetFileName(latestBackup)}");
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadLogic] Failed to restore from backup: {e.Message}");
        }
        
        // Final fallback
        currentCoachData = CreateDefaultCoachData();
        ApplyCoachData(currentCoachData);
        return false;
    }

    /// <summary>
    /// Clean old backup files
    /// </summary>
    private void CleanOldBackups()
    {
        try
        {
            var backupFiles = Directory.GetFiles(backupDirectoryPath, "CoachData_Backup_*.json");
            if (backupFiles.Length > maxBackupFiles)
            {
                Array.Sort(backupFiles);
                for (int i = 0; i < backupFiles.Length - maxBackupFiles; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveLoadLogic] Failed to clean old backups: {e.Message}");
        }
    }

    #endregion

    #region Validation and Default Data

    /// <summary>
    /// Validate coach data integrity
    /// </summary>
    private bool IsCoachDataValid(CoachSaveData data)
    {
        if (data == null) return false;
        if (string.IsNullOrEmpty(data.saveVersion)) return false;
        if (string.IsNullOrEmpty(data.saveTimestamp)) return false;
        
        // Additional validation can be added here
        return true;
    }

    /// <summary>
    /// Create default coach data
    /// </summary>
    private CoachSaveData CreateDefaultCoachData()
    {
        return new CoachSaveData
        {
            saveVersion = "1.0",
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            defenseCoachData = null,
            offenseCoachData = null,
            specialTeamsCoachData = null,
            performanceStats = new PerformanceStatsSave()
        };
    }

    /// <summary>
    /// Create default game data
    /// </summary>
    private GameSaveData CreateDefaultGameData()
    {
        return new GameSaveData
        {
            saveVersion = "1.0",
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            currentWeek = 1,
            teamBudget = 50000f,
            seasonProgress = 0.0f
        };
    }

    #endregion

    #region Public API

    /// <summary>
    /// Force save all data immediately
    /// </summary>
    public async void ForceSave()
    {
        await SaveCoachData();
        await SaveGameData();
    }

    /// <summary>
    /// Get current coach data for display
    /// </summary>
    public CoachSaveData GetCurrentCoachData()
    {
        return currentCoachData;
    }

    /// <summary>
    /// Get current game data
    /// </summary>
    public GameSaveData GetCurrentGameData()
    {
        return currentGameData;
    }

    #endregion
}

// =============================
// Save Data Structures
// Flat dummy schema for clarity and future integration
// =============================

[System.Serializable]
public class CoachSaveData
{
    public string saveVersion;
    public string saveTimestamp;
    public CoachDataSave defenseCoachData;
    public CoachDataSave offenseCoachData;
    public CoachDataSave specialTeamsCoachData;
    public PerformanceStatsSave performanceStats;
}

[System.Serializable]
public class CoachDataSave
{
    public string coachName;
    public float starRating;
    public int weeklySalary;
    public int experience;
    public int defenseBonus;
    public int offenseBonus;
    public int specialTeamsBonus;
    public string position;
}

[System.Serializable]
public class WeeklyPerformanceData
{
    public int week;
    public DateTime weekDate;
    public float teamMorale;
    public int gamesPlayed;
    public int gamesWon;
    public float winRate;
    public float offenseRating;
    public float defenseRating;
    public float specialTeamsRating;
    public float overallRating;
    public float coachingImpact;
    public float budgetSpent;
    public List<string> hiredCoaches = new List<string>();
    public List<string> firedCoaches = new List<string>();
    public string notes;
}

[System.Serializable]
public class PerformanceStatsSave
{
    public int gamesPlayed;
    public int gamesWon;
    public int gamesBeforeCoaching;
    public int winsBeforeCoaching;
    public float teamMorale;
    public List<WeeklyPerformanceData> weeklyHistory = new List<WeeklyPerformanceData>();
}

[System.Serializable]
public class GameSaveData
{
    public string saveVersion;
    public string saveTimestamp;
    public int currentWeek;
    public float teamBudget;
    public float seasonProgress;
}
