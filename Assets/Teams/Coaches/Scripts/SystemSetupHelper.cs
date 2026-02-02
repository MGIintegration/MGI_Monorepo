using UnityEngine;

/// <summary>
/// Helper script to automatically set up all required system components
/// Add this to any GameObject and it will create missing system instances
/// </summary>
public class SystemSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupAllSystems();
        }
    }
    
    [ContextMenu("Setup All Systems")]
    public void SetupAllSystems()
    {
        Debug.Log("[SystemSetupHelper] Setting up all required systems...");
        
        // Setup CoachManager
        if (CoachManager.instance == null)
        {
            GameObject coachManagerGO = new GameObject("CoachManager");
            coachManagerGO.AddComponent<CoachManager>();
            Debug.Log("[SystemSetupHelper] ✅ Created CoachManager");
        }
        
        // Setup SaveLoadLogic
        if (SaveLoadLogic.Instance == null)
        {
            GameObject saveLoadGO = new GameObject("SaveLoadLogic");
            saveLoadGO.AddComponent<SaveLoadLogic>();
            Debug.Log("[SystemSetupHelper] ✅ Created SaveLoadLogic");
        }
        
        // Setup RuntimeValidator
        if (RuntimeValidator.Instance == null)
        {
            GameObject validatorGO = new GameObject("RuntimeValidator");
            validatorGO.AddComponent<RuntimeValidator>();
            Debug.Log("[SystemSetupHelper] ✅ Created RuntimeValidator");
        }
        
        // Setup StatusDeltaChecker
        if (StatusDeltaChecker.Instance == null)
        {
            GameObject deltaGO = new GameObject("StatusDeltaChecker");
            deltaGO.AddComponent<StatusDeltaChecker>();
            Debug.Log("[SystemSetupHelper] ✅ Created StatusDeltaChecker");
        }
        
        Debug.Log("[SystemSetupHelper] 🎉 All systems setup complete!");
    }
    
    [ContextMenu("Check System Status")]
    public void CheckSystemStatus()
    {
        Debug.Log("=== SYSTEM STATUS ===");
        Debug.Log($"CoachManager: {(CoachManager.instance != null ? "✅ Present" : "❌ Missing")}");
        Debug.Log($"SaveLoadLogic: {(SaveLoadLogic.Instance != null ? "✅ Present" : "❌ Missing")}");
        Debug.Log($"RuntimeValidator: {(RuntimeValidator.Instance != null ? "✅ Present" : "❌ Missing")}");
        Debug.Log($"StatusDeltaChecker: {(StatusDeltaChecker.Instance != null ? "✅ Present" : "❌ Missing")}");
        Debug.Log("==================");
    }
}
