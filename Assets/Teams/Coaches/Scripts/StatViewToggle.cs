using UnityEngine;

public class StatViewToggle : MonoBehaviour
{
    [Header("UI Containers")]
    public GameObject coachingStatsContainer;
    public GameObject weeklyBreakdownContainer;
    
    [Header("Database Integration")]
    public StatCardPopulator statCardPopulator; // Reference for database refresh

    void Start()
    {
        // Initial visibility
        ShowCoachingStats();
    }

    public void ShowCoachingStats()
    {
        coachingStatsContainer.SetActive(true);
        weeklyBreakdownContainer.SetActive(false);
    }

    public void ShowWeeklyStats()
    {
        coachingStatsContainer.SetActive(false);
        weeklyBreakdownContainer.SetActive(true);
    }
    
    /// <summary>
    /// Refresh performance data from database
    /// </summary>
    public void RefreshPerformanceData()
    {
        if (statCardPopulator != null)
        {
            // Trigger database reload in StatCardPopulator
            statCardPopulator.LoadPerformanceDataFromDatabase();
        }
    }
}