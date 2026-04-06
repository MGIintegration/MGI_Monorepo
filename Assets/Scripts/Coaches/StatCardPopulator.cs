using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

public class StatCardPopulator : MonoBehaviour
{
    [Header("Prefabs & Containers")]
    public GameObject statRowPrefab;
    public Transform beforeContainer;
    public Transform afterContainer;
    public Transform weeklyBreakdownContainer;
    public GameObject weeklyTextPrefab;

    [Header("Sprites")]
    public Sprite arrowUp;
    public Sprite arrowDown;
    public Sprite offenseIcon;
    public Sprite defenseIcon;
    public Sprite specialTeamsIcon;
    public Sprite winRateIcon;

    [Header("Data")]
    public List<StatEntry> stats = new();
    public List<string> weeklyBreakdownLines = new();
    public WeeklySummaryData summaryData = new();

    [Header("Weekly Summary Texts")]
    public TMP_Text investmentLine;
    public TMP_Text bonusLine;
    public TMP_Text conclusionLine;

    // Configurable sizes
    [Header("Visual Settings")]
    public Vector2 iconSize = new Vector2(30, 30);
    public Vector2 arrowSize = new Vector2(30, 30);
    public float labelFontSize = 30f;
    public float valueFontSize = 30f;
    public float weeklyFontSize = 14f;

    void Start()
    {
        LoadPerformanceDataFromDatabase();
        // Note: PopulateUI() is now called from RefreshUI() in LoadPerformanceDataFromDatabase()
    }

    /// <summary>
    /// ELT Pattern: Extract → Load → Transform performance data from database
    /// Following CoachingStats_v4.cs ROI approach with Dhruv's clean structure
    /// </summary>
    public void LoadPerformanceDataFromDatabase()
    {
        // Clear existing UI first
        ClearAllContainers();
        
        // Extract team and hired coach data from database
        var teamData = ExtractTeamDataFromDatabase();
        var hiredCoaches = ExtractHiredCoachesFromDatabase(); // Only hired coaches, not all coaches
        
        // Load and Transform data into UI format
        stats = TransformToPerformanceStats(teamData, hiredCoaches);
        weeklyBreakdownLines = CreateWeeklyBreakdown(teamData, hiredCoaches);
        summaryData = CreateSummaryData(teamData, hiredCoaches);
        
        // Refresh UI with new data
        RefreshUI();
        
        Debug.Log($"[StatCardPopulator] Loaded {stats.Count} performance stats with {hiredCoaches.Count} hired coaches");
    }

    /// <summary>
    /// Clear all UI containers before refreshing
    /// </summary>
    private void ClearAllContainers()
    {
        if (beforeContainer != null)
        {
            foreach (Transform child in beforeContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        if (afterContainer != null)
        {
            foreach (Transform child in afterContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        if (weeklyBreakdownContainer != null)
        {
            foreach (Transform child in weeklyBreakdownContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Refresh UI with current data
    /// </summary>
    private void RefreshUI()
    {
        ClearAllContainers();
        PopulateUI();
    }

    /// <summary>
    /// Extract team data from database
    /// </summary>
    private TeamDatabaseRecord ExtractTeamDataFromDatabase()
    {
        try
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Database", "team.json");
            
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning("[StatCardPopulator] Team database not found, using defaults");
                return CreateDefaultTeamRecord();
            }

            string jsonContent = File.ReadAllText(jsonPath);
            string wrappedJson = $"{{\"Items\":{jsonContent}}}";
            var wrapper = JsonUtility.FromJson<TeamJsonWrapper>(wrappedJson);
            
            if (wrapper?.Items != null && wrapper.Items.Length > 0)
            {
                return wrapper.Items[0]; // Get first team
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[StatCardPopulator] Failed to load team data: {e.Message}");
        }
        
        return CreateDefaultTeamRecord();
    }

    /// <summary>
    /// Extract only hired coaches from CoachManager (not all coaches from database)
    /// </summary>
    private List<CoachDatabaseRecord> ExtractHiredCoachesFromDatabase()
    {
        var hiredCoaches = new List<CoachDatabaseRecord>();
        
        // Try to get hired coaches from CoachManager first
        var coachManager = FindObjectOfType<CoachManager>();
        if (coachManager != null)
        {
            // Add defense coach if hired
            if (coachManager.defenseCoach != null)
            {
                hiredCoaches.Add(ConvertCoachDataToRecord(coachManager.defenseCoach, "D"));
            }
            
            // Add offense coach if hired
            if (coachManager.offenseCoach != null)
            {
                hiredCoaches.Add(ConvertCoachDataToRecord(coachManager.offenseCoach, "O"));
            }
            
            // Add special teams coach if hired
            if (coachManager.SpecialCoach != null)
            {
                hiredCoaches.Add(ConvertCoachDataToRecord(coachManager.SpecialCoach, "S"));
            }
            
            Debug.Log($"[StatCardPopulator] Found {hiredCoaches.Count} hired coaches from CoachManager");
        }
        
        // If no coaches found from CoachManager, load from database for testing
        if (hiredCoaches.Count == 0)
        {
            Debug.LogWarning("[StatCardPopulator] No hired coaches found, loading sample coaches from database for testing");
            hiredCoaches = LoadSampleHiredCoachesFromDatabase();
        }
        
        return hiredCoaches;
    }

    /// <summary>
    /// Load sample hired coaches from database for testing when CoachManager has no hired coaches
    /// </summary>
    private List<CoachDatabaseRecord> LoadSampleHiredCoachesFromDatabase()
    {
        var hiredCoaches = new List<CoachDatabaseRecord>();
        
        try
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Database", "coach.json");
            
            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                string wrappedJson = $"{{\"Items\":{jsonContent}}}";
                var wrapper = JsonUtility.FromJson<JsonWrapper>(wrappedJson);
                
                if (wrapper?.Items != null && wrapper.Items.Length > 0)
                {
                    // Take first 2-3 coaches from database as "hired" for testing
                    var allCoaches = wrapper.Items.ToList();
                    
                    // Get one coach of each type if available
                    var defenseCoach = allCoaches.FirstOrDefault(c => c.coach_type?.ToUpper() == "D");
                    var offenseCoach = allCoaches.FirstOrDefault(c => c.coach_type?.ToUpper() == "O");
                    var specialTeamsCoach = allCoaches.FirstOrDefault(c => c.coach_type?.ToUpper() == "S");
                    
                    if (defenseCoach != null) hiredCoaches.Add(defenseCoach);
                    if (offenseCoach != null) hiredCoaches.Add(offenseCoach);
                    if (specialTeamsCoach != null) hiredCoaches.Add(specialTeamsCoach);
                    
                    // If we still don't have any, just take the first 2 coaches
                    if (hiredCoaches.Count == 0)
                    {
                        hiredCoaches.AddRange(allCoaches.Take(2));
                    }
                    
                    Debug.Log($"[StatCardPopulator] Loaded {hiredCoaches.Count} sample hired coaches from database");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[StatCardPopulator] Failed to load coaches from database: {e.Message}");
        }
        
        // Final fallback - create sample coaches if database loading failed
        if (hiredCoaches.Count == 0)
        {
            hiredCoaches.Add(CreateSampleCoach("Mike Johnson", "D", 4.0f, 2500000f));
            hiredCoaches.Add(CreateSampleCoach("Sarah Wilson", "O", 3.5f, 2000000f));
            Debug.Log("[StatCardPopulator] Using fallback sample coaches");
        }
        
        return hiredCoaches;
    }

    /// <summary>
    /// Create sample coach for testing when CoachManager is not available
    /// </summary>
    private CoachDatabaseRecord CreateSampleCoach(string name, string type, float rating, float salary)
    {
        return new CoachDatabaseRecord
        {
            coach_name = name,
            coach_type = type,
            overall_rating = rating,
            salary = salary / 1000000f, // Convert to millions
            experience = 5,
            run_defence = rating,
            pressure_control = rating,
            coverage_discipline = rating,
            turnover = rating,
            passing_efficiency = rating,
            rush = rating,
            red_zone_conversion = rating,
            play_variation = rating,
            field_goal_accuracy = rating,
            kickoff_instance = rating,
            return_speed = rating,
            return_coverage = rating
        };
    }

    /// <summary>
    /// Convert CoachData to CoachDatabaseRecord format
    /// </summary>
    private CoachDatabaseRecord ConvertCoachDataToRecord(CoachData coach, string coachType)
    {
        return new CoachDatabaseRecord
        {
            coach_name = coach.coachName,
            coach_type = coachType,
            overall_rating = coach.starRating,
            salary = coach.weeklySalary / 1000000f, // Convert to millions
            experience = coach.experience,
            
            // Use CoachData bonus values for specific stats
            run_defence = coach.defenseBonus / 5f,
            pressure_control = coach.defenseBonus / 5f,
            coverage_discipline = coach.defenseBonus / 5f,
            turnover = coach.defenseBonus / 5f,
            
            passing_efficiency = coach.offenseBonus / 5f,
            rush = coach.offenseBonus / 5f,
            red_zone_conversion = coach.offenseBonus / 5f,
            play_variation = coach.offenseBonus / 5f,
            
            field_goal_accuracy = coach.specialTeamsBonus / 5f,
            kickoff_instance = coach.specialTeamsBonus / 5f,
            return_speed = coach.specialTeamsBonus / 5f,
            return_coverage = coach.specialTeamsBonus / 5f
        };
    }

    /// <summary>
    /// Transform database data into performance statistics
    /// Following CoachingStats_v4.cs methodology for ROI calculation
    /// </summary>
    private List<StatEntry> TransformToPerformanceStats(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        var performanceStats = new List<StatEntry>();

        // Win Rate Performance (inspired by CoachingStats_v4.GetWinRateDelta)
        int baselineWinRate = 42; // Baseline without coaching
        int currentWinRate = CalculateCurrentWinRate(team, coaches);
        
        performanceStats.Add(new StatEntry
        {
            stat = "Win Rate %",
            beforeValue = baselineWinRate,
            afterValue = currentWinRate,
            icon = winRateIcon
        });

        // Offensive Rating
        int baselineOffense = 40;
        int currentOffense = CalculateOffenseRating(team, coaches);
        
        performanceStats.Add(new StatEntry
        {
            stat = "Offense",
            beforeValue = baselineOffense,
            afterValue = currentOffense,
            icon = offenseIcon
        });

        // Defensive Rating  
        int baselineDefense = 38;
        int currentDefense = CalculateDefenseRating(team, coaches);
        
        performanceStats.Add(new StatEntry
        {
            stat = "Defense",
            beforeValue = baselineDefense,
            afterValue = currentDefense,
            icon = defenseIcon
        });

        // Special Teams Rating
        int baselineSpecialTeams = 35;
        int currentSpecialTeams = CalculateSpecialTeamsRating(team, coaches);
        
        performanceStats.Add(new StatEntry
        {
            stat = "Special Teams",
            beforeValue = baselineSpecialTeams,
            afterValue = currentSpecialTeams,
            icon = specialTeamsIcon
        });

        // Weekly Investment
        /*int baselineInvestment = 0;
        int currentInvestment = CalculateWeeklyInvestment(coaches);
        
        performanceStats.Add(new StatEntry
        {
            stat = "Weekly Investment",
            beforeValue = baselineInvestment,
            afterValue = currentInvestment
        });*/

        return performanceStats;
    }

    /// <summary>
    /// Create weekly breakdown with emoji support
    /// </summary>
    private List<string> CreateWeeklyBreakdown(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        var lines = new List<string>();
        
        lines.Add($"Week 1 - {team.team_name} baseline established");
        lines.Add($"Week 2 - Budget: ${team.budget:F1}M allocated");
        
        int week = 3;
        foreach (var coach in coaches.Take(4)) // Show up to 4 coaches
        {
            string coachTypeText = GetCoachTypeText(coach.coach_type);
            int impact = CalculateCoachImpact(coach);
            lines.Add($"Week {week} - {coachTypeText} {coach.coach_name}: +{impact}% boost");
            week++;
        }
        
        // Add performance summary
        if (coaches.Count > 0)
        {
            float avgRating = coaches.Average(c => c.overall_rating);
            int totalImpact = Mathf.RoundToInt((avgRating - 2.5f) * 15f);
            lines.Add($"Week {week} - Combined Impact: +{totalImpact}%");
        }
        
        return lines;
    }

    /// <summary>
    /// Create summary data following CoachingStats_v4.cs ROI methodology
    /// </summary>
    private WeeklySummaryData CreateSummaryData(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        int weeklyInvestment = CalculateWeeklyInvestment(coaches);
        float performanceGain = CalculatePerformanceGain(coaches);
        int playoffBonus = CalculatePlayoffBonus(team, coaches);

        return new WeeklySummaryData
        {
            weeklyInvestment = weeklyInvestment,
            performanceGainPercent = performanceGain,
            playoffBonus = playoffBonus
        };
    }

    /// <summary>
    /// Dhruv's original clean UI population method - simplified
    /// </summary>
    void PopulateUI()
    {
        Debug.Log($"[StatCardPopulator] PopulateUI started with {stats.Count} stats, {weeklyBreakdownLines.Count} weekly lines");

        // Populate performance stats rows
        foreach (var stat in stats)
        {
            if (beforeContainer == null || afterContainer == null || statRowPrefab == null)
            {
                Debug.LogError("[StatCardPopulator] Missing UI references - check Inspector assignments");
                return;
            }

            // === BEFORE ROW ===
            GameObject beforeRow = Instantiate(statRowPrefab, beforeContainer);
            SetRowUI(beforeRow, stat.stat, stat.icon, stat.beforeValue, null);

            // === AFTER ROW ===
            GameObject afterRow = Instantiate(statRowPrefab, afterContainer);

            Sprite arrowSprite = null;
            if (stat.afterValue > stat.beforeValue)
                arrowSprite = arrowUp;
            else if (stat.afterValue < stat.beforeValue)
                arrowSprite = arrowDown;

            SetRowUI(afterRow, stat.stat, stat.icon, stat.afterValue, arrowSprite);
        }

        // Populate weekly breakdown - simplified approach
        if (weeklyBreakdownContainer != null && weeklyTextPrefab != null)
        {
            foreach (string line in weeklyBreakdownLines)
            {
                GameObject entry = Instantiate(weeklyTextPrefab, weeklyBreakdownContainer);

                // Simple approach - just set the text directly
                TMP_Text textComponent = entry.GetComponent<TMP_Text>();
                if (textComponent != null)
                {
                    textComponent.text = line;
                    textComponent.fontSize = weeklyFontSize;
                }
                else
                {
                    // Fallback - try to find text component in children
                    TMP_Text[] textComponents = entry.GetComponentsInChildren<TMP_Text>();
                    if (textComponents.Length > 0)
                    {
                        textComponents[0].text = line;
                        textComponents[0].fontSize = weeklyFontSize;
                    }
                }
            }
        }

        // Populate summary section - ensure text components exist
        if (investmentLine != null)
            investmentLine.text = $"Weekly Investment: ${summaryData.weeklyInvestment:N0} | Performance Gain: +{summaryData.performanceGainPercent:F1}%";
        
        if (bonusLine != null)
            bonusLine.text = $"Playoff Bonus Potential: ${summaryData.playoffBonus:N0}";

        bool isProfitable = summaryData.performanceGainPercent > 0 && summaryData.playoffBonus > 0;

        if (conclusionLine != null)
        {
            conclusionLine.text = isProfitable
                ? "COACHING IS PROFITABLE"
                : "COACHING IS NOT PROFITABLE";
            conclusionLine.color = isProfitable ? Color.green : Color.red;
        }

        Debug.Log($"[StatCardPopulator] PopulateUI completed successfully. Profitability: {isProfitable}");
    }

    void SetRowUI(GameObject row, string label, Sprite icon, int value, Sprite arrowSprite)
    {
        // Set label text + font size - more robust finding
        var labelText = row.transform.Find("Stat")?.GetComponent<TMP_Text>();
        if (labelText != null)
        {
            labelText.text = label;
            labelText.fontSize = labelFontSize;
        }

        // Set value text + font size - more robust finding
        var valueText = row.transform.Find("Number")?.GetComponent<TMP_Text>();
        if (valueText != null)
        {
            valueText.text = value.ToString();
            valueText.fontSize = valueFontSize;
        }

        // Set icon + size - more robust finding
        var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImg != null && icon != null)
        {
            iconImg.sprite = icon;
            iconImg.rectTransform.sizeDelta = iconSize;
        }

        // Handle arrow logic (optional) - more robust finding
        var arrowObj = row.transform.Find("UpDown");
        if (arrowObj != null)
        {
            var arrowImage = arrowObj.GetComponent<Image>();
            if (arrowImage != null && arrowSprite != null)
            {
                arrowImage.sprite = arrowSprite;
                arrowImage.rectTransform.sizeDelta = arrowSize;
                arrowObj.gameObject.SetActive(true);
            }
            else
            {
                arrowObj.gameObject.SetActive(false);
            }
        }
    }

    #region Performance Calculation Methods (Following CoachingStats_v4.cs)

    /// <summary>
    /// Calculate current win rate based on team and coaching improvements
    /// </summary>
    private int CalculateCurrentWinRate(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        int baselineWinRate = 42;
        float teamBonus = team.overall_rating * 8f; // Team quality contributes
        float coachBonus = coaches.Sum(c => (c.overall_rating - 2.5f) * 6f); // Coach improvement
        
        int currentWinRate = Mathf.RoundToInt(baselineWinRate + teamBonus + coachBonus);
        return Mathf.Clamp(currentWinRate, 0, 95); // Cap at realistic values
    }

    /// <summary>
    /// Calculate offense rating based on team and offensive coaches
    /// </summary>
    private int CalculateOffenseRating(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        int baseRating = 40;
        float teamContribution = team.offence_rating * 12f;
        
        var offenseCoaches = coaches.Where(c => c.coach_type?.ToUpper() == "O").ToList();
        float coachContribution = offenseCoaches.Sum(c => c.overall_rating * 8f);
        
        int totalRating = Mathf.RoundToInt(baseRating + teamContribution + coachContribution);
        return Mathf.Clamp(totalRating, 0, 90);
    }

    /// <summary>
    /// Calculate defense rating based on team and defensive coaches
    /// </summary>
    private int CalculateDefenseRating(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        int baseRating = 38;
        float teamContribution = team.defence_rating * 12f;
        
        var defenseCoaches = coaches.Where(c => c.coach_type?.ToUpper() == "D").ToList();
        float coachContribution = defenseCoaches.Sum(c => c.overall_rating * 8f);
        
        int totalRating = Mathf.RoundToInt(baseRating + teamContribution + coachContribution);
        return Mathf.Clamp(totalRating, 0, 90);
    }

    /// <summary>
    /// Calculate special teams rating based on team and special teams coaches
    /// </summary>
    private int CalculateSpecialTeamsRating(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        int baseRating = 35;
        float teamContribution = team.special_teams_rating * 12f;
        
        var specialTeamsCoaches = coaches.Where(c => c.coach_type?.ToUpper() == "S").ToList();
        float coachContribution = specialTeamsCoaches.Sum(c => c.overall_rating * 8f);
        
        int totalRating = Mathf.RoundToInt(baseRating + teamContribution + coachContribution);
        return Mathf.Clamp(totalRating, 0, 90);
    }

    /// <summary>
    /// Calculate total weekly investment in coaching
    /// </summary>
    private int CalculateWeeklyInvestment(List<CoachDatabaseRecord> coaches)
    {
        float totalSalary = coaches.Sum(c => c.salary);
        float weeklyInvestment = (totalSalary * 1000000f) / 52f; // Convert to weekly
        return Mathf.RoundToInt(weeklyInvestment);
    }

    /// <summary>
    /// Calculate performance gain percentage (following CoachingStats_v4.GetWinRateDelta)
    /// </summary>
    private float CalculatePerformanceGain(List<CoachDatabaseRecord> coaches)
    {
        if (coaches.Count == 0) return 0f;
        
        float avgRating = coaches.Average(c => c.overall_rating);
        float performanceGain = (avgRating - 2.5f) * 12f; // Scale improvement
        
        return Mathf.Max(0f, performanceGain);
    }

    /// <summary>
    /// Calculate playoff bonus potential (following CoachingStats_v4.GetPerformanceROI)
    /// </summary>
    private int CalculatePlayoffBonus(TeamDatabaseRecord team, List<CoachDatabaseRecord> coaches)
    {
        float performanceGain = CalculatePerformanceGain(coaches);
        
        // Enhanced ROI calculation following CoachingStats_v4.cs methodology
        if (performanceGain <= 0) return 0;
        
        // Base playoff bonus scaled by team budget and performance improvement
        float baseBonusPercentage = 15f; // 15% improvement threshold for profitability
        float bonusMultiplier = performanceGain / baseBonusPercentage; // How much above threshold
        
        float teamRevenuePotential = team.budget * 1000000f * 0.2f; // 20% of budget as revenue potential
        float playoffBonus = teamRevenuePotential * bonusMultiplier;
        
        return Mathf.RoundToInt(Mathf.Max(0f, playoffBonus));
    }

    /// <summary>
    /// Calculate individual coach impact
    /// </summary>
    private int CalculateCoachImpact(CoachDatabaseRecord coach)
    {
        float impact = (coach.overall_rating - 2.5f) * 8f; // Scale from 2.5-5.0 to percentage
        return Mathf.RoundToInt(Mathf.Max(0f, impact));
    }

    /// <summary>
    /// Get text description for coach type
    /// </summary>
    private string GetCoachTypeText(string coachType)
    {
        switch (coachType?.ToUpper())
        {
            case "D": return "Defense";
            case "O": return "Offense";
            case "S": return "Special Teams";
            default: return "Coach";
        }
    }

    /// <summary>
    /// Create default team record for fallback
    /// </summary>
    private TeamDatabaseRecord CreateDefaultTeamRecord()
    {
        return new TeamDatabaseRecord
        {
            team_name = "Default Team",
            league = "Practice League",
            budget = 25.0f,
            overall_rating = 2.5f,
            defence_rating = 2.5f,
            offence_rating = 2.5f,
            special_teams_rating = 2.5f,
            description = "Default team for testing"
        };
    }

    #endregion
}