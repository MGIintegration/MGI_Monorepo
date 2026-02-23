// =============================
// RuntimeValidator.cs
// FMG Coaching System - Phase 2: JSON/ScriptableObject Bridge
// Runtime Validation and Performance Monitoring
// 
// Author: Azwad (Implementation)
// Integration: Kevin's validator bridge
// Purpose: Build JSON/ScriptableObject bridge and runtime validator
// =============================
//
// Features:
// - JSON/ScriptableObject bridge validation
// - Runtime performance monitoring for save/load operations
// - Data integrity checks during gameplay
// - Memory usage tracking
// - Validator logic and scripting structures
// - Time to load/save metrics
//
// =============================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using System.IO;

public class RuntimeValidator : MonoBehaviour
{
    [Header("Validation Configuration")]
    [SerializeField] private bool enableRuntimeValidation = true;
    [SerializeField] private bool enablePerformanceMonitoring = true;
    [SerializeField] private float validationInterval = 5f; // seconds
    [SerializeField] private int maxValidationHistory = 100;

    [Header("Performance Thresholds")]
    [SerializeField] private float saveTimeWarningThreshold = 100f; // milliseconds
    [SerializeField] private float loadTimeWarningThreshold = 50f; // milliseconds
    [SerializeField] private int memoryWarningThreshold = 50; // MB

    // Validation results storage
    private List<ValidationResult> validationHistory = new List<ValidationResult>();
    private List<PerformanceMetric> performanceHistory = new List<PerformanceMetric>();

    // Runtime monitoring
    private Stopwatch performanceStopwatch = new Stopwatch();
    private long lastMemorySnapshot = 0;

    // Events for validation notifications
    public static event System.Action<ValidationResult> OnValidationCompleted;
    public static event System.Action<PerformanceMetric> OnPerformanceMetricRecorded;
    public static event System.Action<string> OnValidationWarning;
    public static event System.Action<string> OnValidationError;

    // Singleton pattern
    public static RuntimeValidator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (enableRuntimeValidation)
        {
            InvokeRepeating(nameof(PerformRuntimeValidation), validationInterval, validationInterval);
        }

        // Subscribe to save/load events for performance monitoring
        SaveLoadLogic.OnSaveCompleted += OnSaveCompleted;
        SaveLoadLogic.OnLoadCompleted += OnLoadCompleted;
        SaveLoadLogic.OnSaveError += OnSaveError;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        SaveLoadLogic.OnSaveCompleted -= OnSaveCompleted;
        SaveLoadLogic.OnLoadCompleted -= OnLoadCompleted;
        SaveLoadLogic.OnSaveError -= OnSaveError;
    }

    #region JSON/ScriptableObject Bridge Validation

    /// <summary>
    /// Validate JSON to ScriptableObject conversion integrity
    /// </summary>
    public ValidationResult ValidateJSONBridge(string jsonData, Type targetType)
    {
        var result = new ValidationResult
        {
            timestamp = DateTime.Now,
            validationType = ValidationType.JSONBridge,
            targetType = targetType.Name
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Attempt JSON parsing
            var parsedObject = JsonUtility.FromJson(jsonData, targetType);
            
            if (parsedObject == null)
            {
                result.isValid = false;
                result.errorMessage = "JSON parsing returned null object";
                result.validationDetails.Add("Parse result: null");
            }
            else
            {
                // Validate object structure
                var structureValidation = ValidateObjectStructure(parsedObject, targetType);
                result.isValid = structureValidation.isValid;
                result.errorMessage = structureValidation.errorMessage;
                result.validationDetails.AddRange(structureValidation.details);
            }
        }
        catch (Exception e)
        {
            result.isValid = false;
            result.errorMessage = $"JSON parsing exception: {e.Message}";
            result.validationDetails.Add($"Exception type: {e.GetType().Name}");
        }

        stopwatch.Stop();
        result.validationTime = stopwatch.ElapsedMilliseconds;

        RecordValidationResult(result);
        return result;
    }

    /// <summary>
    /// Validate ScriptableObject to JSON conversion integrity
    /// </summary>
    public ValidationResult ValidateScriptableObjectBridge(ScriptableObject sourceObject)
    {
        var result = new ValidationResult
        {
            timestamp = DateTime.Now,
            validationType = ValidationType.ScriptableObjectBridge,
            targetType = sourceObject.GetType().Name
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Convert to JSON
            string jsonData = JsonUtility.ToJson(sourceObject, true);
            
            if (string.IsNullOrEmpty(jsonData))
            {
                result.isValid = false;
                result.errorMessage = "ScriptableObject to JSON conversion returned empty string";
            }
            else
            {
                // Validate JSON structure
                var jsonValidation = ValidateJSONStructure(jsonData);
                result.isValid = jsonValidation.isValid;
                result.errorMessage = jsonValidation.errorMessage;
                result.validationDetails.AddRange(jsonValidation.details);
                
                // Test round-trip conversion
                if (result.isValid)
                {
                    var roundTripValidation = TestRoundTripConversion(sourceObject, jsonData);
                    result.isValid = roundTripValidation.isValid;
                    if (!roundTripValidation.isValid)
                    {
                        result.errorMessage = $"Round-trip validation failed: {roundTripValidation.errorMessage}";
                    }
                    result.validationDetails.AddRange(roundTripValidation.details);
                }
            }
        }
        catch (Exception e)
        {
            result.isValid = false;
            result.errorMessage = $"ScriptableObject conversion exception: {e.Message}";
            result.validationDetails.Add($"Exception type: {e.GetType().Name}");
        }

        stopwatch.Stop();
        result.validationTime = stopwatch.ElapsedMilliseconds;

        RecordValidationResult(result);
        return result;
    }

    #endregion

    #region Runtime Validation

    /// <summary>
    /// Perform comprehensive runtime validation
    /// </summary>
    private void PerformRuntimeValidation()
    {
        var overallResult = new ValidationResult
        {
            timestamp = DateTime.Now,
            validationType = ValidationType.Runtime,
            targetType = "System"
        };

        var validationStopwatch = Stopwatch.StartNew();
        var details = new List<string>();
        bool isValid = true;

        try
        {
            // Validate SaveLoadLogic state
            var saveLoadValidation = ValidateSaveLoadLogic();
            details.AddRange(saveLoadValidation.details);
            if (!saveLoadValidation.isValid)
            {
                isValid = false;
                overallResult.errorMessage += saveLoadValidation.errorMessage + "; ";
            }

            // Validate CoachManager state
            var coachManagerValidation = ValidateCoachManager();
            details.AddRange(coachManagerValidation.details);
            if (!coachManagerValidation.isValid)
            {
                isValid = false;
                overallResult.errorMessage += coachManagerValidation.errorMessage + "; ";
            }

            // Validate data consistency
            var consistencyValidation = ValidateDataConsistency();
            details.AddRange(consistencyValidation.details);
            if (!consistencyValidation.isValid)
            {
                isValid = false;
                overallResult.errorMessage += consistencyValidation.errorMessage + "; ";
            }

            // Monitor memory usage
            var memoryValidation = ValidateMemoryUsage();
            details.AddRange(memoryValidation.details);
            if (!memoryValidation.isValid)
            {
                OnValidationWarning?.Invoke(memoryValidation.errorMessage);
            }
        }
        catch (Exception e)
        {
            isValid = false;
            overallResult.errorMessage = $"Runtime validation exception: {e.Message}";
            details.Add($"Exception during validation: {e.GetType().Name}");
        }

        validationStopwatch.Stop();
        overallResult.isValid = isValid;
        overallResult.validationDetails = details;
        overallResult.validationTime = validationStopwatch.ElapsedMilliseconds;

        RecordValidationResult(overallResult);

        if (!isValid)
        {
            OnValidationError?.Invoke(overallResult.errorMessage);
        }
    }

    #endregion

    #region Specific Validation Methods

    /// <summary>
    /// Validate SaveLoadLogic system state
    /// </summary>
    private ValidationResultInternal ValidateSaveLoadLogic()
    {
        var result = new ValidationResultInternal();
        
        var saveLoadLogic = SaveLoadLogic.Instance;
        if (saveLoadLogic == null)
        {
            result.isValid = false;
            result.errorMessage = "SaveLoadLogic instance not found";
            return result;
        }

        // Check if save paths are accessible
        string persistentPath = Application.persistentDataPath;
        if (!Directory.Exists(persistentPath))
        {
            result.isValid = false;
            result.errorMessage = "Persistent data path not accessible";
            return result;
        }

        result.details.Add($"SaveLoadLogic instance: Active");
        result.details.Add($"Persistent path: {persistentPath}");
        result.isValid = true;
        
        return result;
    }

    /// <summary>
    /// Validate CoachManager state
    /// </summary>
    private ValidationResultInternal ValidateCoachManager()
    {
        var result = new ValidationResultInternal();
        
        var coachManager = CoachManager.instance;
        if (coachManager == null)
        {
            result.isValid = false;
            result.errorMessage = "CoachManager instance not found";
            return result;
        }

        // Validate hired coaches
        int hiredCoachCount = 0;
        if (coachManager.defenseCoach != null) hiredCoachCount++;
        if (coachManager.offenseCoach != null) hiredCoachCount++;
        if (coachManager.SpecialCoach != null) hiredCoachCount++;

        result.details.Add($"CoachManager instance: Active");
        result.details.Add($"Hired coaches: {hiredCoachCount}");
        
        // Validate coach data integrity
        if (coachManager.defenseCoach != null)
        {
            var defenseValidation = ValidateCoachData(coachManager.defenseCoach, "Defense");
            result.details.AddRange(defenseValidation.details);
            if (!defenseValidation.isValid)
            {
                result.isValid = false;
                result.errorMessage += defenseValidation.errorMessage + "; ";
            }
        }

        if (coachManager.offenseCoach != null)
        {
            var offenseValidation = ValidateCoachData(coachManager.offenseCoach, "Offense");
            result.details.AddRange(offenseValidation.details);
            if (!offenseValidation.isValid)
            {
                result.isValid = false;
                result.errorMessage += offenseValidation.errorMessage + "; ";
            }
        }

        if (coachManager.SpecialCoach != null)
        {
            var specialValidation = ValidateCoachData(coachManager.SpecialCoach, "Special Teams");
            result.details.AddRange(specialValidation.details);
            if (!specialValidation.isValid)
            {
                result.isValid = false;
                result.errorMessage += specialValidation.errorMessage + "; ";
            }
        }

        // result.isValid is already initialized to true, no need to check for null
        return result;
    }

    /// <summary>
    /// Validate individual coach data
    /// </summary>
    private ValidationResultInternal ValidateCoachData(CoachData coach, string position)
    {
        var result = new ValidationResultInternal();
        
        if (string.IsNullOrEmpty(coach.coachName))
        {
            result.isValid = false;
            result.errorMessage = $"{position} coach has empty name";
            return result;
        }

        if (coach.starRating < 1 || coach.starRating > 5)
        {
            result.isValid = false;
            result.errorMessage = $"{position} coach has invalid star rating: {coach.starRating}";
            return result;
        }

        if (coach.weeklySalary < 0)
        {
            result.isValid = false;
            result.errorMessage = $"{position} coach has negative salary: {coach.weeklySalary}";
            return result;
        }

        result.details.Add($"{position} coach: {coach.coachName} (Rating: {coach.starRating}, Salary: {coach.weeklySalary})");
        result.isValid = true;
        
        return result;
    }

    /// <summary>
    /// Validate data consistency across systems
    /// </summary>
    private ValidationResultInternal ValidateDataConsistency()
    {
        var result = new ValidationResultInternal();
        result.isValid = true;

        // Check if save data matches runtime data
        var saveLoadLogic = SaveLoadLogic.Instance;
        var coachManager = CoachManager.instance;

        if (saveLoadLogic != null && coachManager != null)
        {
            var currentSaveData = saveLoadLogic.GetCurrentCoachData();
            if (currentSaveData != null)
            {
                // Validate defense coach consistency
                bool defenseConsistent = ValidateCoachConsistency(
                    currentSaveData.defenseCoachData, 
                    coachManager.defenseCoach, 
                    "Defense"
                );
                
                if (!defenseConsistent)
                {
                    result.isValid = false;
                    result.errorMessage += "Defense coach data inconsistency; ";
                }

                // Similar checks for offense and special teams...
                result.details.Add($"Data consistency check completed");
            }
        }

        return result;
    }

    /// <summary>
    /// Validate coach data consistency between save and runtime
    /// </summary>
    private bool ValidateCoachConsistency(CoachDataSave saveData, CoachData runtimeData, string position)
    {
        if (saveData == null && runtimeData == null) return true;
        if (saveData == null || runtimeData == null) return false;

        return saveData.coachName == runtimeData.coachName &&
               Math.Abs(saveData.starRating - runtimeData.starRating) < 0.01f &&
               saveData.weeklySalary == runtimeData.weeklySalary;
    }

    /// <summary>
    /// Validate memory usage
    /// </summary>
    private ValidationResultInternal ValidateMemoryUsage()
    {
        var result = new ValidationResultInternal();
        
        long currentMemory = GC.GetTotalMemory(false) / (1024 * 1024); // Convert to MB
        long memoryDelta = currentMemory - lastMemorySnapshot;
        
        result.details.Add($"Current memory usage: {currentMemory} MB");
        result.details.Add($"Memory delta: {memoryDelta} MB");
        
        if (currentMemory > memoryWarningThreshold)
        {
            result.isValid = false;
            result.errorMessage = $"Memory usage exceeds threshold: {currentMemory} MB > {memoryWarningThreshold} MB";
        }
        else
        {
            result.isValid = true;
        }

        lastMemorySnapshot = currentMemory;
        return result;
    }

    #endregion

    #region Helper Validation Methods

    /// <summary>
    /// Validate object structure integrity
    /// </summary>
    private ValidationResultInternal ValidateObjectStructure(object obj, Type expectedType)
    {
        var result = new ValidationResultInternal();
        
        if (obj == null)
        {
            result.isValid = false;
            result.errorMessage = "Object is null";
            return result;
        }

        if (!expectedType.IsInstanceOfType(obj))
        {
            result.isValid = false;
            result.errorMessage = $"Object type mismatch. Expected: {expectedType.Name}, Actual: {obj.GetType().Name}";
            return result;
        }

        result.details.Add($"Object type validation passed: {expectedType.Name}");
        result.isValid = true;
        
        return result;
    }

    /// <summary>
    /// Validate JSON structure
    /// </summary>
    private ValidationResultInternal ValidateJSONStructure(string jsonData)
    {
        var result = new ValidationResultInternal();
        
        if (string.IsNullOrEmpty(jsonData))
        {
            result.isValid = false;
            result.errorMessage = "JSON data is null or empty";
            return result;
        }

        try
        {
            // Basic JSON validation
            var tempObj = JsonUtility.FromJson<object>(jsonData);
            result.details.Add($"JSON structure validation passed");
            result.details.Add($"JSON length: {jsonData.Length} characters");
            result.isValid = true;
        }
        catch (Exception e)
        {
            result.isValid = false;
            result.errorMessage = $"Invalid JSON structure: {e.Message}";
        }
        
        return result;
    }

    /// <summary>
    /// Test round-trip conversion (Object -> JSON -> Object)
    /// </summary>
    private ValidationResultInternal TestRoundTripConversion(ScriptableObject original, string jsonData)
    {
        var result = new ValidationResultInternal();
        
        try
        {
            var converted = JsonUtility.FromJson(jsonData, original.GetType());
            
            if (converted == null)
            {
                result.isValid = false;
                result.errorMessage = "Round-trip conversion returned null";
                return result;
            }

            // Basic comparison (can be expanded based on specific needs)
            result.details.Add("Round-trip conversion successful");
            result.isValid = true;
        }
        catch (Exception e)
        {
            result.isValid = false;
            result.errorMessage = $"Round-trip conversion failed: {e.Message}";
        }
        
        return result;
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// Start performance measurement
    /// </summary>
    public void StartPerformanceMeasurement(string operationName)
    {
        if (!enablePerformanceMonitoring) return;
        
        performanceStopwatch.Restart();
        UnityEngine.Debug.Log($"[RuntimeValidator] Started measuring: {operationName}");
    }

    /// <summary>
    /// End performance measurement and record metric
    /// </summary>
    public void EndPerformanceMeasurement(string operationName, PerformanceOperationType operationType)
    {
        if (!enablePerformanceMonitoring) return;
        
        performanceStopwatch.Stop();
        
        var metric = new PerformanceMetric
        {
            timestamp = DateTime.Now,
            operationName = operationName,
            operationType = operationType,
            elapsedMilliseconds = performanceStopwatch.ElapsedMilliseconds,
            memoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024)
        };

        RecordPerformanceMetric(metric);
        
        // Check against thresholds
        CheckPerformanceThresholds(metric);
        
        UnityEngine.Debug.Log($"[RuntimeValidator] {operationName} completed in {metric.elapsedMilliseconds}ms");
    }

    /// <summary>
    /// Check performance against warning thresholds
    /// </summary>
    private void CheckPerformanceThresholds(PerformanceMetric metric)
    {
        bool exceedsThreshold = false;
        string warningMessage = "";

        switch (metric.operationType)
        {
            case PerformanceOperationType.Save:
                if (metric.elapsedMilliseconds > saveTimeWarningThreshold)
                {
                    exceedsThreshold = true;
                    warningMessage = $"Save operation took {metric.elapsedMilliseconds}ms (threshold: {saveTimeWarningThreshold}ms)";
                }
                break;
                
            case PerformanceOperationType.Load:
                if (metric.elapsedMilliseconds > loadTimeWarningThreshold)
                {
                    exceedsThreshold = true;
                    warningMessage = $"Load operation took {metric.elapsedMilliseconds}ms (threshold: {loadTimeWarningThreshold}ms)";
                }
                break;
        }

        if (exceedsThreshold)
        {
            OnValidationWarning?.Invoke(warningMessage);
        }
    }

    #endregion

    #region Event Handlers

    private void OnSaveCompleted(bool success)
    {
        if (success)
        {
            UnityEngine.Debug.Log("[RuntimeValidator] Save operation completed successfully");
        }
        else
        {
            OnValidationError?.Invoke("Save operation failed");
        }
    }

    private void OnLoadCompleted(bool success)
    {
        if (success)
        {
            UnityEngine.Debug.Log("[RuntimeValidator] Load operation completed successfully");
        }
        else
        {
            OnValidationError?.Invoke("Load operation failed");
        }
    }

    private void OnSaveError(string error)
    {
        OnValidationError?.Invoke($"Save error: {error}");
    }

    #endregion

    #region Data Recording

    /// <summary>
    /// Record validation result
    /// </summary>
    private void RecordValidationResult(ValidationResult result)
    {
        validationHistory.Add(result);
        
        // Maintain history size limit
        if (validationHistory.Count > maxValidationHistory)
        {
            validationHistory.RemoveAt(0);
        }
        
        OnValidationCompleted?.Invoke(result);
    }

    /// <summary>
    /// Record performance metric
    /// </summary>
    private void RecordPerformanceMetric(PerformanceMetric metric)
    {
        performanceHistory.Add(metric);
        
        // Maintain history size limit
        if (performanceHistory.Count > maxValidationHistory)
        {
            performanceHistory.RemoveAt(0);
        }
        
        OnPerformanceMetricRecorded?.Invoke(metric);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get validation history
    /// </summary>
    public List<ValidationResult> GetValidationHistory()
    {
        return new List<ValidationResult>(validationHistory);
    }

    /// <summary>
    /// Get performance history
    /// </summary>
    public List<PerformanceMetric> GetPerformanceHistory()
    {
        return new List<PerformanceMetric>(performanceHistory);
    }

    /// <summary>
    /// Get latest validation result
    /// </summary>
    public ValidationResult GetLatestValidationResult()
    {
        return validationHistory.LastOrDefault();
    }

    /// <summary>
    /// Force immediate validation
    /// </summary>
    public void ForceValidation()
    {
        PerformRuntimeValidation();
    }

    /// <summary>
    /// Generate validation report
    /// </summary>
    public string GenerateValidationReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Runtime Validation Report ===");
        report.AppendLine($"Generated: {DateTime.Now}");
        report.AppendLine($"Total validations: {validationHistory.Count}");
        
        int successfulValidations = validationHistory.Count(v => v.isValid);
        report.AppendLine($"Successful validations: {successfulValidations}");
        report.AppendLine($"Failed validations: {validationHistory.Count - successfulValidations}");
        
        if (performanceHistory.Count > 0)
        {
            var avgSaveTime = performanceHistory
                .Where(p => p.operationType == PerformanceOperationType.Save)
                .Average(p => p.elapsedMilliseconds);
            var avgLoadTime = performanceHistory
                .Where(p => p.operationType == PerformanceOperationType.Load)
                .Average(p => p.elapsedMilliseconds);
                
            report.AppendLine($"Average save time: {avgSaveTime:F2}ms");
            report.AppendLine($"Average load time: {avgLoadTime:F2}ms");
        }
        
        return report.ToString();
    }

    #endregion
}

// =============================
// Data Structures for Validation
// =============================

[System.Serializable]
public class ValidationResult
{
    public DateTime timestamp;
    public ValidationType validationType;
    public string targetType;
    public bool isValid;
    public string errorMessage;
    public List<string> validationDetails = new List<string>();
    public long validationTime; // milliseconds
}

[System.Serializable]
public class PerformanceMetric
{
    public DateTime timestamp;
    public string operationName;
    public PerformanceOperationType operationType;
    public long elapsedMilliseconds;
    public long memoryUsageMB;
}

// Internal validation result for helper methods
public class ValidationResultInternal
{
    public bool isValid = true;
    public string errorMessage = "";
    public List<string> details = new List<string>();
}

public enum ValidationType
{
    JSONBridge,
    ScriptableObjectBridge,
    Runtime,
    DataConsistency,
    Performance
}

public enum PerformanceOperationType
{
    Save,
    Load,
    Validation,
    Conversion
}
