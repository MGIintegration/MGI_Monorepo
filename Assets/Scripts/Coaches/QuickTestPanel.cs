// =============================
// QuickTestPanel.cs
// Simple UI panel for testing coaching system components
// Add this to a Canvas GameObject for easy testing
// =============================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuickTestPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button testSystemsButton;
    [SerializeField] private Button saveDataButton;
    [SerializeField] private Button loadDataButton;
    [SerializeField] private Button forceValidationButton;
    [SerializeField] private Button checkDeltasButton;
    [SerializeField] private Text statusText;
    [SerializeField] private ScrollRect logScrollRect;
    [SerializeField] private Text logText;
    
    [Header("Test Settings")]
    [SerializeField] private bool createButtonsAutomatically = true;
    
    private void Start()
    {
        if (createButtonsAutomatically)
        {
            CreateTestButtons();
        }
        
        SetupButtonListeners();
        UpdateStatus("🎮 Quick Test Panel Ready");
    }
    
    /// <summary>
    /// Create test buttons automatically if not assigned
    /// </summary>
    private void CreateTestButtons()
    {
        if (testSystemsButton == null) testSystemsButton = CreateButton("Test All Systems", new Vector2(0, 100));
        if (saveDataButton == null) saveDataButton = CreateButton("Save Data", new Vector2(0, 50));
        if (loadDataButton == null) loadDataButton = CreateButton("Load Data", new Vector2(0, 0));
        if (forceValidationButton == null) forceValidationButton = CreateButton("Force Validation", new Vector2(0, -50));
        if (checkDeltasButton == null) checkDeltasButton = CreateButton("Check Deltas", new Vector2(0, -100));
        
        if (statusText == null) statusText = CreateText("Status: Ready", new Vector2(0, 150));
        if (logText == null) logText = CreateText("Logs will appear here...", new Vector2(0, -200));
    }
    
    /// <summary>
    /// Create a button GameObject
    /// </summary>
    private Button CreateButton(string text, Vector2 position)
    {
        GameObject buttonObj = new GameObject(text + " Button");
        buttonObj.transform.SetParent(transform);
        
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 30);
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Add text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        return button;
    }
    
    /// <summary>
    /// Create a text GameObject
    /// </summary>
    private Text CreateText(string text, Vector2 position)
    {
        GameObject textObj = new GameObject(text + " Text");
        textObj.transform.SetParent(transform);
        
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(400, 30);
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = 16;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        return textComponent;
    }
    
    /// <summary>
    /// Setup button click listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        if (testSystemsButton != null)
            testSystemsButton.onClick.AddListener(() => StartCoroutine(TestAllSystems()));
            
        if (saveDataButton != null)
            saveDataButton.onClick.AddListener(TestSaveData);
            
        if (loadDataButton != null)
            loadDataButton.onClick.AddListener(TestLoadData);
            
        if (forceValidationButton != null)
            forceValidationButton.onClick.AddListener(TestForceValidation);
            
        if (checkDeltasButton != null)
            checkDeltasButton.onClick.AddListener(TestCheckDeltas);
    }
    
    /// <summary>
    /// Test all systems sequentially
    /// </summary>
    private IEnumerator TestAllSystems()
    {
        UpdateStatus("🧪 Testing All Systems...");
        AddLog("=== Starting Comprehensive Test ===");
        
        // Test 1: System instances
        yield return StartCoroutine(TestSystemInstances());
        yield return new WaitForSeconds(1f);
        
        // Test 2: Save/Load
        yield return StartCoroutine(TestSaveLoadSystem());
        yield return new WaitForSeconds(1f);
        
        // Test 3: Validation
        yield return StartCoroutine(TestValidationSystem());
        yield return new WaitForSeconds(1f);
        
        // Test 4: Delta tracking
        yield return StartCoroutine(TestDeltaSystem());
        yield return new WaitForSeconds(1f);
        
        UpdateStatus("✅ All Tests Complete");
        AddLog("=== Test Sequence Finished ===");
    }
    
    /// <summary>
    /// Test system instances
    /// </summary>
    private IEnumerator TestSystemInstances()
    {
        UpdateStatus("Testing System Instances...");
        
        bool saveLoadExists = SaveLoadLogic.Instance != null;
        bool validatorExists = RuntimeValidator.Instance != null;
        bool deltaExists = StatusDeltaChecker.Instance != null;
        
        AddLog($"SaveLoadLogic: {(saveLoadExists ? "✅ Found" : "❌ Missing")}");
        AddLog($"RuntimeValidator: {(validatorExists ? "✅ Found" : "❌ Missing")}");
        AddLog($"StatusDeltaChecker: {(deltaExists ? "✅ Found" : "❌ Missing")}");
        
        bool allFound = saveLoadExists && validatorExists && deltaExists;
        AddLog($"System Instances: {(allFound ? "✅ PASS" : "❌ FAIL")}");
        
        yield return null;
    }
    
    /// <summary>
    /// Test save/load system
    /// </summary>
    private IEnumerator TestSaveLoadSystem()
    {
        UpdateStatus("Testing Save/Load System...");
        
        try
        {
            if (SaveLoadLogic.Instance != null)
            {
                var testData = SaveLoadLogic.Instance.GetCurrentCoachData();
                AddLog($"Data Gathering: {(testData != null ? "✅ Success" : "⚠️ No Data")}");
                
                // Test save (don't actually save, just verify method exists)
                bool hasSaveMethod = SaveLoadLogic.Instance.GetType().GetMethod("SaveCoachData") != null;
                AddLog($"Save Method: {(hasSaveMethod ? "✅ Available" : "❌ Missing")}");
            }
            else
            {
                AddLog("❌ SaveLoadLogic instance not found");
            }
        }
        catch (System.Exception e)
        {
            AddLog($"❌ Save/Load Test Error: {e.Message}");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// Test validation system
    /// </summary>
    private IEnumerator TestValidationSystem()
    {
        UpdateStatus("Testing Validation System...");
        
        try
        {
            if (RuntimeValidator.Instance != null)
            {
                RuntimeValidator.Instance.ForceValidation();
                AddLog("✅ Force Validation Executed");
                
                var history = RuntimeValidator.Instance.GetValidationHistory();
                AddLog($"Validation History: {history.Count} records");
            }
            else
            {
                AddLog("❌ RuntimeValidator instance not found");
            }
        }
        catch (System.Exception e)
        {
            AddLog($"❌ Validation Test Error: {e.Message}");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// Test delta system
    /// </summary>
    private IEnumerator TestDeltaSystem()
    {
        UpdateStatus("Testing Delta System...");
        
        try
        {
            if (StatusDeltaChecker.Instance != null)
            {
                StatusDeltaChecker.Instance.SetBaselineStats();
                AddLog("✅ Baseline Stats Set");
                
                var deltaHistory = StatusDeltaChecker.Instance.GetDeltaHistory();
                AddLog($"Delta History: {deltaHistory.Count} records");
                
                StatusDeltaChecker.Instance.ForceDeltaCheck();
                AddLog("✅ Force Delta Check Executed");
            }
            else
            {
                AddLog("❌ StatusDeltaChecker instance not found");
            }
        }
        catch (System.Exception e)
        {
            AddLog($"❌ Delta Test Error: {e.Message}");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// Test save data operation
    /// </summary>
    private async void TestSaveData()
    {
        UpdateStatus("Testing Save Operation...");
        
        try
        {
            if (SaveLoadLogic.Instance != null)
            {
                bool result = await SaveLoadLogic.Instance.SaveCoachData();
                AddLog($"✅ Save Data command executed - Result: {result}");
            }
            else
            {
                AddLog("❌ SaveLoadLogic instance not found");
            }
        }
        catch (System.Exception e)
        {
            AddLog($"❌ Save Error: {e.Message}");
        }
    }
    
    /// <summary>
    /// Test load data operation
    /// </summary>
    private async void TestLoadData()
    {
        UpdateStatus("Testing Load Operation...");
        
        try
        {
            if (SaveLoadLogic.Instance != null)
            {
                bool result = await SaveLoadLogic.Instance.LoadCoachData();
                AddLog($"✅ Load Data command executed - Result: {result}");
            }
            else
            {
                AddLog("❌ SaveLoadLogic instance not found");
            }
        }
        catch (System.Exception e)
        {
            AddLog($"❌ Load Error: {e.Message}");
        }
    }
    
    /// <summary>
    /// Test force validation
    /// </summary>
    private void TestForceValidation()
    {
        UpdateStatus("Testing Force Validation...");
        
        try
        {
            if (RuntimeValidator.Instance != null)
            {
                RuntimeValidator.Instance.ForceValidation();
                AddLog("✅ Force Validation executed");
            }
            else
            {
                AddLog("❌ RuntimeValidator instance not found");
            }
        }
        catch (System.Exception e)
        {
            AddLog($"❌ Validation Error: {e.Message}");
        }
    }
    
    /// <summary>
    /// Test delta checking
    /// </summary>
    private void TestCheckDeltas()
    {
        UpdateStatus("Testing Delta Checking...");
        
        try
        {
            if (StatusDeltaChecker.Instance != null)
            {
                StatusDeltaChecker.Instance.ForceDeltaCheck();
                var history = StatusDeltaChecker.Instance.GetDeltaHistory();
                AddLog($"✅ Delta check complete. History: {history.Count} records");
            }
            else
            {
                AddLog("❌ StatusDeltaChecker instance not found");
            }
        }
        catch (System.Exception e)
        {
            AddLog($"❌ Delta Error: {e.Message}");
        }
    }
    
    /// <summary>
    /// Update status text
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"Status: {message}";
        }
        
        Debug.Log($"[QuickTestPanel] {message}");
    }
    
    /// <summary>
    /// Add log entry
    /// </summary>
    private void AddLog(string message)
    {
        if (logText != null)
        {
            logText.text += $"\n{System.DateTime.Now:HH:mm:ss} - {message}";
            
            // Auto-scroll to bottom
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0;
            }
        }
        
        Debug.Log($"[QuickTestPanel] {message}");
    }
    
    /// <summary>
    /// Clear log
    /// </summary>
    [ContextMenu("Clear Log")]
    public void ClearLog()
    {
        if (logText != null)
        {
            logText.text = "Log cleared...";
        }
    }
}
