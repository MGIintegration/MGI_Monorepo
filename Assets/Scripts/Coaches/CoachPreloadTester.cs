using UnityEngine;
using System.Collections;

/// <summary>
/// Simple testing script for verifying coach pre-loading functionality
/// Attach this to any GameObject in the FMGCOACH scene for testing
/// </summary>
public class CoachPreloadTester : MonoBehaviour
{
    [Header("Testing Controls")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log("[CoachPreloadTester] Starting coach pre-load test...");
            
            // Check if CoachManager exists and provide helpful message
            if (CoachManager.instance == null)
            {
                Debug.LogWarning("[CoachPreloadTester] ⚠️ CoachManager not found in scene!");
                Debug.LogWarning("💡 Solutions:");
                Debug.LogWarning("1. Add CoachManager script to a GameObject in this scene");
                Debug.LogWarning("2. Switch to FMGCOACH.unity scene where CoachManager exists");
                Debug.LogWarning("3. The CoachPreloadTester will not function without CoachManager");
            }
            else
            {
                Debug.Log("[CoachPreloadTester] ✅ CoachManager found! Press P/R/F keys to test.");
            }
        }
    }
    
    private void Update()
    {
        // Only process input if CoachManager exists
        if (CoachManager.instance == null)
        {
            return; // Silently return without spamming logs
        }
        
        // Test controls
        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintCoachStatus();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadCoaches();
        }
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            FireAllCoaches();
        }
    }
    
    /// <summary>
    /// Print current coach status to console
    /// </summary>
    private void PrintCoachStatus()
    {
        if (CoachManager.instance == null)
        {
            Debug.LogWarning("[CoachPreloadTester] CoachManager instance not found!");
            return;
        }
        
        Debug.Log("=== CURRENT COACH STATUS ===");
        
        if (CoachManager.instance.defenseCoach != null)
        {
            var coach = CoachManager.instance.defenseCoach;
            Debug.Log($"Defense Coach: {coach.coachName} | Rating: {coach.starRating} stars | Salary: ${coach.weeklySalary:N0}/week | DEF: +{coach.defenseBonus} | OFF: +{coach.offenseBonus}");
        }
        else
        {
            Debug.Log("Defense Coach: EMPTY SLOT");
        }
        
        if (CoachManager.instance.offenseCoach != null)
        {
            var coach = CoachManager.instance.offenseCoach;
            Debug.Log($"Offense Coach: {coach.coachName} | Rating: {coach.starRating} stars | Salary: ${coach.weeklySalary:N0}/week | DEF: +{coach.defenseBonus} | OFF: +{coach.offenseBonus}");
        }
        else
        {
            Debug.Log("Offense Coach: EMPTY SLOT");
        }
        
        Debug.Log("=== END STATUS ===");
    }
    
    /// <summary>
    /// Force reload coaches from API/database
    /// </summary>
    private void ReloadCoaches()
    {
        if (CoachManager.instance == null)
        {
            Debug.LogWarning("[CoachPreloadTester] CoachManager instance not found!");
            return;
        }
        
        Debug.Log("[CoachPreloadTester] Reloading coaches...");
        
        // Fire existing coaches first
        FireAllCoaches();
        
        // Force reload by calling the private method through reflection or manually trigger it
        var coachManager = CoachManager.instance;
        if (coachManager != null)
        {
            // Re-initialize the system
            var method = typeof(CoachManager).GetMethod("PreLoadTeamCoaches", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                var coroutine = method.Invoke(coachManager, null) as System.Collections.IEnumerator;
                if (coroutine != null)
                {
                    StartCoroutine(coroutine);
                }
            }
            else
            {
                Debug.LogError("[CoachPreloadTester] Could not find PreLoadTeamCoaches method");
            }
        }
    }
    
    /// <summary>
    /// Fire all current coaches
    /// </summary>
    private void FireAllCoaches()
    {
        if (CoachManager.instance == null)
        {
            Debug.LogWarning("[CoachPreloadTester] CoachManager instance not found!");
            return;
        }
        
        Debug.Log("[CoachPreloadTester] Firing all coaches...");
        
        if (CoachManager.instance.defenseCoach != null)
        {
            CoachManager.instance.FireCoach(CoachType.Defense);
        }
        
        if (CoachManager.instance.offenseCoach != null)
        {
            CoachManager.instance.FireCoach(CoachType.Offense);
        }
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        // Display instructions on screen
        GUI.Box(new Rect(10, 10, 300, 100), "Coach Pre-load Tester");
        GUI.Label(new Rect(20, 35, 280, 20), "P - Print coach status to console");
        GUI.Label(new Rect(20, 55, 280, 20), "R - Reload coaches from API/DB");
        GUI.Label(new Rect(20, 75, 280, 20), "F - Fire all coaches");
        
        // Show current status
        GUI.Box(new Rect(10, 120, 300, 80), "Current Status");
        string defenseStatus = CoachManager.instance?.defenseCoach?.coachName ?? "EMPTY";
        string offenseStatus = CoachManager.instance?.offenseCoach?.coachName ?? "EMPTY";
        
        GUI.Label(new Rect(20, 145, 280, 20), $"Defense: {defenseStatus}");
        GUI.Label(new Rect(20, 165, 280, 20), $"Offense: {offenseStatus}");
    }
}
