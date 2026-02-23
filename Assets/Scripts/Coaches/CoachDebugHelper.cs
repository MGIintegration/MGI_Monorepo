using UnityEngine;

/// <summary>
/// Simple debug script to help diagnose coach loading issues
/// Attach this to any GameObject in the scene and check the console
/// </summary>
public class CoachDebugHelper : MonoBehaviour
{
    private void Start()
    {
        // Wait a moment for initialization, then check status
        Invoke(nameof(CheckCoachStatus), 2.0f);
    }
    
    private void Update()
    {
        // Press C to check coach status
        if (Input.GetKeyDown(KeyCode.C))
        {
            CheckCoachStatus();
        }
        
        // Press L to check loadFromAPI setting
        if (Input.GetKeyDown(KeyCode.L))
        {
            CheckAPISettings();
        }
    }
    
    private void CheckCoachStatus()
    {
        Debug.Log("=== COACH DEBUG STATUS ===");
        
        if (CoachManager.instance == null)
        {
            Debug.LogError("❌ CoachManager.instance is NULL! Make sure CoachManager is in the scene.");
            return;
        }
        
        Debug.Log($"✅ CoachManager found");
        
        // Check API settings using reflection since the fields are private
        var coachManagerType = typeof(CoachManager);
        var loadFromAPIField = coachManagerType.GetField("loadFromAPI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var baseURLField = coachManagerType.GetField("baseURL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (loadFromAPIField != null)
        {
            bool loadFromAPI = (bool)loadFromAPIField.GetValue(CoachManager.instance);
            Debug.Log($"loadFromAPI = {loadFromAPI}");
            
            if (!loadFromAPI)
            {
                Debug.LogWarning("⚠️ loadFromAPI is FALSE! This is why you're seeing ScriptableObject coaches (Dhruv, Adity)");
                Debug.Log("💡 Solution: Set loadFromAPI = true in CoachManager inspector");
            }
        }
        
        if (baseURLField != null)
        {
            string baseURL = (string)baseURLField.GetValue(CoachManager.instance);
            Debug.Log($"baseURL = {baseURL}");
        }
        
        // Check current coaches
        Debug.Log("--- Current Coaches ---");
        if (CoachManager.instance.defenseCoach != null)
        {
            Debug.Log($"Defense: {CoachManager.instance.defenseCoach.coachName}");
        }
        else
        {
            Debug.Log("Defense: EMPTY");
        }
        
        if (CoachManager.instance.offenseCoach != null)
        {
            Debug.Log($"Offense: {CoachManager.instance.offenseCoach.coachName}");
        }
        else
        {
            Debug.Log("Offense: EMPTY");
        }
        
        Debug.Log("=== END DEBUG STATUS ===");
    }
    
    private void CheckAPISettings()
    {
        Debug.Log("=== API SETTINGS CHECK ===");
        
        if (CoachManager.instance == null)
        {
            Debug.LogError("❌ CoachManager.instance is NULL!");
            return;
        }
        
        var coachManagerType = typeof(CoachManager);
        var loadFromAPIField = coachManagerType.GetField("loadFromAPI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isAPIAvailableField = coachManagerType.GetField("isAPIAvailable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (loadFromAPIField != null)
        {
            bool loadFromAPI = (bool)loadFromAPIField.GetValue(CoachManager.instance);
            Debug.Log($"loadFromAPI: {loadFromAPI}");
            
            if (!loadFromAPI)
            {
                Debug.LogError("❌ PROBLEM FOUND: loadFromAPI is FALSE!");
                Debug.Log("🔧 SOLUTION: In Unity Inspector, find CoachManager component and set 'Load From API' to TRUE");
            }
        }
        
        if (isAPIAvailableField != null)
        {
            bool isAPIAvailable = (bool)isAPIAvailableField.GetValue(CoachManager.instance);
            Debug.Log($"isAPIAvailable: {isAPIAvailable}");
        }
        
        Debug.Log("=== END API SETTINGS ===");
    }
    
    private void OnGUI()
    {
        GUI.Box(new Rect(10, 220, 300, 60), "Coach Debug Helper");
        GUI.Label(new Rect(20, 245, 280, 20), "C - Check coach status");
        GUI.Label(new Rect(20, 260, 280, 20), "L - Check API settings");
    }
}
