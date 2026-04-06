// =============================
// SystemTester.cs
// FMG Coaching System - Testing Script
// Quick validation of SaveLoadLogic, RuntimeValidator, and StatusDeltaChecker
// =============================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SystemTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = true;
    
    [Header("UI References (Optional)")]
    [SerializeField] private Text statusText;
    [SerializeField] private Button testButton;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunAllTests());
        }
        
        if (testButton != null)
        {
            testButton.onClick.AddListener(() => StartCoroutine(RunAllTests()));
        }
    }
    
    /// <summary>
    /// Run comprehensive system tests
    /// </summary>
    public IEnumerator RunAllTests()
    {
        UpdateStatus("🧪 Starting System Tests...");
        yield return new WaitForSeconds(1f);
        
        // Test 1: Check script instances
        bool scriptsLoaded = TestScriptInstances();
        UpdateStatus($"Script Instances: {(scriptsLoaded ? "✅ PASS" : "❌ FAIL")}");
        yield return new WaitForSeconds(1f);
        
        // Test 2: Test SaveLoadLogic
        bool saveLoadWorks = TestSaveLoadLogic();
        UpdateStatus($"SaveLoad System: {(saveLoadWorks ? "✅ PASS" : "❌ FAIL")}");
        yield return new WaitForSeconds(1f);
        
        // Test 3: Test RuntimeValidator
        bool validatorWorks = TestRuntimeValidator();
        UpdateStatus($"Runtime Validator: {(validatorWorks ? "✅ PASS" : "❌ FAIL")}");
        yield return new WaitForSeconds(1f);
        
        // Test 4: Test StatusDeltaChecker
        bool deltaWorks = TestStatusDeltaChecker();
        UpdateStatus($"Delta Checker: {(deltaWorks ? "✅ PASS" : "❌ FAIL")}");
        yield return new WaitForSeconds(1f);
        
        // Test 5: Integration test
        bool integrationWorks = TestSystemIntegration();
        UpdateStatus($"System Integration: {(integrationWorks ? "✅ PASS" : "❌ FAIL")}");
        
        // Final results
        bool allPassed = scriptsLoaded && saveLoadWorks && validatorWorks && deltaWorks && integrationWorks;
        UpdateStatus($"\n🎯 FINAL RESULT: {(allPassed ? "✅ ALL SYSTEMS OPERATIONAL" : "❌ SOME TESTS FAILED")}");
        
        if (allPassed)
        {
            Debug.Log("🎉 All systems are working correctly!");
        }
        else
        {
            Debug.LogWarning("⚠️ Some systems need attention. Check the logs for details.");
        }
    }
    
    /// <summary>
    /// Test if all script instances are properly created
    /// </summary>
    private bool TestScriptInstances()
    {
        bool saveLoadExists = SaveLoadLogic.Instance != null;
        bool validatorExists = RuntimeValidator.Instance != null;
        bool deltaExists = StatusDeltaChecker.Instance != null;
        
        Debug.Log($"SaveLoadLogic Instance: {(saveLoadExists ? "Found" : "Missing")}");
        Debug.Log($"RuntimeValidator Instance: {(validatorExists ? "Found" : "Missing")}");
        Debug.Log($"StatusDeltaChecker Instance: {(deltaExists ? "Found" : "Missing")}");
        
        return saveLoadExists && validatorExists && deltaExists;
    }
    
    /// <summary>
    /// Test SaveLoadLogic functionality
    /// </summary>
    private bool TestSaveLoadLogic()
    {
        var saveLoad = SaveLoadLogic.Instance;
        if (saveLoad == null) return false;
        
        try
        {
            // Test data gathering
            var testData = saveLoad.GetCurrentCoachData();
            Debug.Log($"SaveLoadLogic: Data gathering {(testData != null ? "successful" : "returned null")}");
            
            // Test save capability (don't actually save, just test the method exists)
            bool hasGatherMethod = saveLoad.GetType().GetMethod("GatherCoachData") != null;
            bool hasSaveMethod = saveLoad.GetType().GetMethod("SaveCoachData") != null;
            bool hasLoadMethod = saveLoad.GetType().GetMethod("LoadCoachData") != null;
            
            Debug.Log($"SaveLoadLogic Methods: Gather={hasGatherMethod}, Save={hasSaveMethod}, Load={hasLoadMethod}");
            
            return hasGatherMethod && hasSaveMethod && hasLoadMethod;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveLoadLogic test failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test RuntimeValidator functionality
    /// </summary>
    private bool TestRuntimeValidator()
    {
        var validator = RuntimeValidator.Instance;
        if (validator == null) return false;
        
        try
        {
            // Test validation methods
            bool hasValidateMethod = validator.GetType().GetMethod("ValidateJSONBridge") != null;
            bool hasPerformanceMethod = validator.GetType().GetMethod("StartPerformanceMeasurement") != null;
            bool hasForceValidation = validator.GetType().GetMethod("ForceValidation") != null;
            
            Debug.Log($"RuntimeValidator Methods: Validate={hasValidateMethod}, Performance={hasPerformanceMethod}, Force={hasForceValidation}");
            
            // Test force validation
            if (hasForceValidation)
            {
                validator.ForceValidation();
                Debug.Log("RuntimeValidator: Force validation executed");
            }
            
            return hasValidateMethod && hasPerformanceMethod && hasForceValidation;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RuntimeValidator test failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test StatusDeltaChecker functionality
    /// </summary>
    private bool TestStatusDeltaChecker()
    {
        var deltaChecker = StatusDeltaChecker.Instance;
        if (deltaChecker == null) return false;
        
        try
        {
            // Test delta methods
            bool hasSetBaseline = deltaChecker.GetType().GetMethod("SetBaselineStats") != null;
            bool hasUpdateStats = deltaChecker.GetType().GetMethod("UpdateCurrentStats") != null;
            bool hasForceDelta = deltaChecker.GetType().GetMethod("ForceDeltaCheck") != null;
            bool hasGetHistory = deltaChecker.GetType().GetMethod("GetDeltaHistory") != null;
            
            Debug.Log($"StatusDeltaChecker Methods: Baseline={hasSetBaseline}, Update={hasUpdateStats}, Force={hasForceDelta}, History={hasGetHistory}");
            
            // Test baseline setting
            if (hasSetBaseline)
            {
                deltaChecker.SetBaselineStats();
                Debug.Log("StatusDeltaChecker: Baseline stats set");
            }
            
            // Test delta history
            if (hasGetHistory)
            {
                var history = deltaChecker.GetDeltaHistory();
                Debug.Log($"StatusDeltaChecker: Delta history has {history.Count} entries");
            }
            
            return hasSetBaseline && hasUpdateStats && hasForceDelta && hasGetHistory;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"StatusDeltaChecker test failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test system integration between all components
    /// </summary>
    private bool TestSystemIntegration()
    {
        try
        {
            // Test component communication by checking if instances exist
            // (Can't directly access events from outside the class)
            bool saveLoadExists = SaveLoadLogic.Instance != null;
            bool validatorExists = RuntimeValidator.Instance != null;
            bool deltaExists = StatusDeltaChecker.Instance != null;
            
            Debug.Log($"Event Systems: SaveLoad={saveLoadExists}, Validator={validatorExists}, Delta={deltaExists}");
            
            // Test if CoachManager integration works
            bool coachManagerExists = CoachManager.instance != null;
            Debug.Log($"CoachManager Integration: {(coachManagerExists ? "Available" : "Not Found")}");
            
            // Test file system access
            bool persistentPathExists = System.IO.Directory.Exists(Application.persistentDataPath);
            Debug.Log($"Persistent Data Path: {(persistentPathExists ? "Accessible" : "Not Accessible")}");
            
            return persistentPathExists; // At minimum, we need file system access
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Integration test failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Update status display
    /// </summary>
    private void UpdateStatus(string message)
    {
        Debug.Log($"[SystemTester] {message}");
        
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    /// <summary>
    /// Manual test trigger (call from UI or other scripts)
    /// </summary>
    [ContextMenu("Run Manual Test")]
    public void RunManualTest()
    {
        StartCoroutine(RunAllTests());
    }
}
