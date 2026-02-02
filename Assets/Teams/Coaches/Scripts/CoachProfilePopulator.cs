using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Linq;
using UnityEngine.Networking;

[System.Serializable]
public class CoachProfileWrapper
{
    public string name;
    public int experience;
    public string previousTeam;
    public int championshipWins;
    public SpecialtyEntry[] specialties;
    public ContractTermEntry[] contractTerms;
}

[System.Serializable]
public class ContractTermEntry
{
    public string key;
    public string value;
}

// API Models for backend integration
[System.Serializable]
public class ApiCoachDetails
{
    public string coachId;
    public string coachName;
    public string coachType;
    public int experience;
    public int from;
    public int to;
    public int gamesPlayed;
    public int gamesWon;
    public int gamesLost;
    public int gamesTie;
    public float winLossPercentage;
    public string currentTeam;
    public int currentTeamGamesPlayed;
    public int currentTeamGamesWon;
    public int currentTeamGamesLost;
    public int currentTeamGamesTie;
    public string prevTeam;
    public int championshipWon;
    public float overall_rating;
    public float salary;
    public int contractLength;
    public float bonus;
    public float totalCost;
    
    // Speciality stats
    public float runDefence;
    public float pressureControl;
    public float coverageDiscipline;
    public float turnover;
    public float passingEfficiency;
    public float rush;
    public float redZoneConversion;
    public float playVariation;
    public float fieldGoalAccuracy;
    public float kickoffInstance;
    public float returnSpeed;
    public float returnCoverage;
}

[System.Serializable]
public class ApiCoachDetailsWrapper
{
    public ApiCoachDetails[] Items;
}

// Additional API Models
[System.Serializable]
public class ApiTeam
{
    public string teamId;
    public string teamName;
    public float budget;
    public float overallRating;
    public float offenceRating;
    public float defenceRating;
    public float specialTeamsRating;
    public string offenceCoach;
    public string defenceCoach;
    public string speacialTeamsCoach;
}

public class CoachProfilePopulator : MonoBehaviour
{
    public CoachProfileWrapper coach;

    [Header("Database Integration")]
    [SerializeField] private bool loadFromDatabase = true;
    [SerializeField] private bool useAPI = true; // New API integration flag
    [SerializeField] private string apiBaseUrl = "http://localhost:5175/api/coach";
    [SerializeField] private string specificCoachId = ""; // For loading specific coach by ID

    [Header("Header Texts")]
    public TMP_Text coachNameText;

    [Header("Coach Metadata")]
    public TMP_Text experienceText;
    public TMP_Text previousTeamText;
    public TMP_Text championshipWinsText;

    [Header("Prefabs & Containers")]
    public GameObject specialityRowPrefab;
    public GameObject contractRowPrefab;
    public Transform specialityContainer;
    public Transform contractRowContainer;

    // Database cache
    private static List<CoachDatabaseRecord> cachedCoaches = null;
    private static bool isCacheInitialized = false;
    
    // Team name cache for API lookups
    private static Dictionary<string, string> teamIdToNameCache = new Dictionary<string, string>();

    // Start is called before the first frame update
    void Start()
    {
        // Initialize coach from database if enabled
        if (loadFromDatabase)
        {
            if (useAPI)
            {
                StartCoroutine(LoadCoachFromAPI());
            }
            else
            {
                LoadCoachFromDatabase();
            }
        }

        // Ensure we have a coach to display
        if (coach == null)
        {
            coach = CreateDefaultCoach();
        }

        PopulateUI();
    }

    /// <summary>
    /// Load coach from database
    /// </summary>
    private void LoadCoachFromDatabase()
    {
        try
        {
            var dbCoach = LoadRandomCoachFromDatabase();
            if (dbCoach != null)
            {
                coach = TransformToCoachProfile(dbCoach);
                Debug.Log($"[CoachProfilePopulator] Loaded coach from database: {coach.name}");
            }
            else
            {
                Debug.LogWarning("[CoachProfilePopulator] No coach loaded from database, using default");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachProfilePopulator] Error loading from database: {e.Message}");
        }
    }

    /// <summary>
    /// Load coach from API with JSON fallback
    /// </summary>
    private IEnumerator LoadCoachFromAPI()
    {
        Debug.Log("[CoachProfilePopulator] Starting API coach load...");
        
        // Test API connection first
        yield return StartCoroutine(TestAPIConnection());
        
        // Pre-populate team names cache
        yield return StartCoroutine(PrePopulateTeamNamesCache());
        
        // Load coaches from API
        yield return StartCoroutine(LoadCoachesFromAPI());
    }

    /// <summary>
    /// Pre-populate team names cache for better performance
    /// </summary>
    private IEnumerator PrePopulateTeamNamesCache()
    {
        Debug.Log("[CoachProfilePopulator] Pre-populating team names cache...");

        // Try to load team names from JSON file first
        yield return StartCoroutine(LoadTeamNamesFromJSON());

        // Then fetch any missing teams from API if needed
        Debug.Log($"[CoachProfilePopulator] Team cache populated with {teamIdToNameCache.Count} teams");
    }

    /// <summary>
    /// Load team names from JSON file for offline fallback
    /// </summary>
    private IEnumerator LoadTeamNamesFromJSON()
    {
        try
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Database", "team.json");
            
            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                string wrappedJson = $"{{\"Items\":{jsonContent}}}";
                var wrapper = JsonUtility.FromJson<TeamJsonWrapper>(wrappedJson);

                if (wrapper?.Items != null)
                {
                    foreach (var team in wrapper.Items)
                    {
                        if (!string.IsNullOrEmpty(team.team_id) && !string.IsNullOrEmpty(team.team_name))
                        {
                            teamIdToNameCache[team.team_id] = team.team_name;
                        }
                    }
                    Debug.Log($"[CoachProfilePopulator] Loaded {wrapper.Items.Length} team names from JSON");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachProfilePopulator] Error loading team names from JSON: {e.Message}");
        }
        
        yield break;
    }

    /// <summary>
    /// Team JSON structure
    /// </summary>
    [System.Serializable]
    public class TeamRecord
    {
        public string team_id;
        public string team_name;
        public float budget;
        public string description;
    }

    [System.Serializable]
    public class TeamJsonWrapper
    {
        public TeamRecord[] Items;
    }

    /// <summary>
    /// JSON wrapper for coach database records
    /// </summary>
    [System.Serializable]
    public class JsonWrapper
    {
        public CoachDatabaseRecord[] Items;
    }

    /// <summary>
    /// Test API connection
    /// </summary>
    private IEnumerator TestAPIConnection()
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/all"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[CoachProfilePopulator] API connection successful");
            }
            else
            {
                Debug.LogWarning($"[CoachProfilePopulator] API connection failed: {request.error}. Falling back to JSON.");
                useAPI = false; // Fallback to JSON
            }
        }
    }

    /// <summary>
    /// Load coaches from API
    /// </summary>
    private IEnumerator LoadCoachesFromAPI()
    {
        if (!useAPI)
        {
            // Fallback to JSON
            LoadCoachFromDatabase();
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/all"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"[CoachProfilePopulator] Received API response: {jsonResponse.Substring(0, Mathf.Min(200, jsonResponse.Length))}...");

                    // Wrap in object for JsonUtility
                    string wrappedJson = $"{{\"Items\":{jsonResponse}}}";
                    var wrapper = JsonUtility.FromJson<ApiCoachDetailsWrapper>(wrappedJson);

                    if (wrapper?.Items != null && wrapper.Items.Length > 0)
                    {
                        ApiCoachDetails selectedCoach;
                        
                        if (!string.IsNullOrEmpty(specificCoachId))
                        {
                            // Load specific coach by ID
                            selectedCoach = System.Array.Find(wrapper.Items, c => c.coachId == specificCoachId);
                            if (selectedCoach == null)
                            {
                                Debug.LogWarning($"[CoachProfilePopulator] Coach with ID {specificCoachId} not found, loading random coach");
                                selectedCoach = wrapper.Items[UnityEngine.Random.Range(0, wrapper.Items.Length)];
                            }
                        }
                        else
                        {
                            // Load random coach
                            selectedCoach = wrapper.Items[UnityEngine.Random.Range(0, wrapper.Items.Length)];
                        }

                        coach = TransformApiCoachToProfile(selectedCoach);
                        PopulateUI();
                        Debug.Log($"[CoachProfilePopulator] Loaded coach from API: {coach.name}");
                    }
                    else
                    {
                        Debug.LogError("[CoachProfilePopulator] No coaches received from API");
                        LoadCoachFromDatabase(); // Fallback
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachProfilePopulator] Error parsing API response: {e.Message}");
                    LoadCoachFromDatabase(); // Fallback
                }
            }
            else
            {
                Debug.LogError($"[CoachProfilePopulator] API request failed: {request.error}");
                LoadCoachFromDatabase(); // Fallback
            }
        }
    }

    /// <summary>
    /// Transform API coach to profile format
    /// </summary>
    private CoachProfileWrapper TransformApiCoachToProfile(ApiCoachDetails apiCoach)
    {
        var profile = new CoachProfileWrapper
        {
            name = apiCoach.coachName,
            experience = apiCoach.experience,
            previousTeam = GetTeamNameFromId(apiCoach.prevTeam),
            championshipWins = apiCoach.championshipWon,
            specialties = TransformApiToSpecialties(apiCoach),
            contractTerms = TransformApiToContractTerms(apiCoach)
        };

        return profile;
    }

    /// <summary>
    /// Get team name from team ID (with caching)
    /// </summary>
    private string GetTeamNameFromId(string teamId)
    {
        if (string.IsNullOrEmpty(teamId))
        {
            return "Free Agent";
        }

        // Check cache first
        if (teamIdToNameCache.ContainsKey(teamId))
        {
            return teamIdToNameCache[teamId];
        }

        // Check if it's already a team name (not a GUID)
        if (!IsGuid(teamId))
        {
            return teamId; // It's already a team name
        }

        // If not in cache, start a coroutine to fetch it
        StartCoroutine(FetchTeamNameCoroutine(teamId));
        
        // Return placeholder while fetching
        return "Loading...";
    }

    /// <summary>
    /// Check if a string is a GUID format
    /// </summary>
    private bool IsGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Fetch team name from API and cache it
    /// </summary>
    private IEnumerator FetchTeamNameCoroutine(string teamId)
    {
        if (string.IsNullOrEmpty(teamId) || teamIdToNameCache.ContainsKey(teamId))
        {
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl.Replace("/coach", "")}/coach/teamDetails/{teamId}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var teamResponse = JsonUtility.FromJson<ApiTeam>(request.downloadHandler.text);
                    if (teamResponse != null && !string.IsNullOrEmpty(teamResponse.teamName))
                    {
                        teamIdToNameCache[teamId] = teamResponse.teamName;
                        Debug.Log($"[CoachProfilePopulator] Cached team name: {teamResponse.teamName} for ID: {teamId}");
                        
                        // Refresh UI if this team was being displayed
                        PopulateUI();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachProfilePopulator] Error parsing team response: {e.Message}");
                    teamIdToNameCache[teamId] = "Unknown Team";
                }
            }
            else
            {
                Debug.LogWarning($"[CoachProfilePopulator] Failed to fetch team name for ID: {teamId}");
                teamIdToNameCache[teamId] = "Unknown Team";
            }
        }
    }

    /// <summary>
    /// Convert API stats to top 4 specialties
    /// </summary>
    private SpecialtyEntry[] TransformApiToSpecialties(ApiCoachDetails apiCoach)
    {
        List<SpecialtyEntry> allSpecialties = new List<SpecialtyEntry>();

        switch (apiCoach.coachType.ToUpper())
        {
            case "D": // Defense Coach
                if (apiCoach.runDefence > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Run Defense", value = CalculateBonus(apiCoach.runDefence) });
                if (apiCoach.pressureControl > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Pressure Control", value = CalculateBonus(apiCoach.pressureControl) });
                if (apiCoach.coverageDiscipline > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Coverage Discipline", value = CalculateBonus(apiCoach.coverageDiscipline) });
                if (apiCoach.turnover > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Turnover Generation", value = CalculateBonus(apiCoach.turnover) });
                break;

            case "O": // Offense Coach
                if (apiCoach.passingEfficiency > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Passing Efficiency", value = CalculateBonus(apiCoach.passingEfficiency) });
                if (apiCoach.rush > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Rushing Attack", value = CalculateBonus(apiCoach.rush) });
                if (apiCoach.redZoneConversion > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Red Zone Conversion", value = CalculateBonus(apiCoach.redZoneConversion) });
                if (apiCoach.playVariation > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Play Variation", value = CalculateBonus(apiCoach.playVariation) });
                break;

            case "S": // Special Teams Coach
                if (apiCoach.fieldGoalAccuracy > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Field Goal Accuracy", value = CalculateBonus(apiCoach.fieldGoalAccuracy) });
                if (apiCoach.kickoffInstance > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Kickoff Distance", value = CalculateBonus(apiCoach.kickoffInstance) });
                if (apiCoach.returnSpeed > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Return Speed", value = CalculateBonus(apiCoach.returnSpeed) });
                if (apiCoach.returnCoverage > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Return Coverage", value = CalculateBonus(apiCoach.returnCoverage) });
                break;
        }

        // Sort by value and take top 4
        allSpecialties.Sort((a, b) => b.value.CompareTo(a.value));
        
        int count = Mathf.Min(4, allSpecialties.Count);
        SpecialtyEntry[] topSpecialties = new SpecialtyEntry[count];
        for (int i = 0; i < count; i++)
        {
            topSpecialties[i] = allSpecialties[i];
        }

        return topSpecialties;
    }

    /// <summary>
    /// Generate exactly 4 contract terms from API data
    /// </summary>
    private ContractTermEntry[] TransformApiToContractTerms(ApiCoachDetails apiCoach)
    {
        float weeklySalary = (apiCoach.salary * 1000000f) / 52f;
        float performanceBonus = apiCoach.bonus * 1000000f; // Bonus is already in millions
        float totalCost = apiCoach.totalCost * 1000000f; // Total cost is already in millions

        return new ContractTermEntry[]
        {
            new ContractTermEntry { key = "Weekly Salary", value = $"${weeklySalary:N0}" },
            new ContractTermEntry { key = "Contract Length", value = $"{apiCoach.contractLength} games minimum" },
            new ContractTermEntry { key = "Performance Bonus", value = $"+${performanceBonus:N0} for 80%+ wins" },
            new ContractTermEntry { key = "Total Cost", value = $"${totalCost:N0}" }
        };
    }

    /// <summary>
    /// Extract coaches from JSON database
    /// </summary>
    private List<CoachDatabaseRecord> ExtractCoachesFromDatabase()
    {
        if (isCacheInitialized && cachedCoaches != null)
        {
            return cachedCoaches;
        }

        try
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Database", "coach.json");
            
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[CoachProfilePopulator] Coach JSON file not found at: {jsonPath}");
                return new List<CoachDatabaseRecord>();
            }

            string jsonContent = File.ReadAllText(jsonPath);
            
            // Wrap array in object for JsonUtility
            string wrappedJson = $"{{\"Items\":{jsonContent}}}";
            var wrapper = JsonUtility.FromJson<JsonWrapper>(wrappedJson);
            
            if (wrapper == null || wrapper.Items == null)
            {
                Debug.LogError("[CoachProfilePopulator] Failed to parse JSON");
                return new List<CoachDatabaseRecord>();
            }
            
            cachedCoaches = new List<CoachDatabaseRecord>(wrapper.Items);
            isCacheInitialized = true;
            
            Debug.Log($"[CoachProfilePopulator] Successfully loaded {wrapper.Items.Length} coaches from JSON");
            return cachedCoaches;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachProfilePopulator] Failed to load from JSON: {e.Message}");
            return new List<CoachDatabaseRecord>();
        }
    }

    /// <summary>
    /// Load random coach from database
    /// </summary>
    private CoachDatabaseRecord LoadRandomCoachFromDatabase()
    {
        var allCoaches = ExtractCoachesFromDatabase();
        
        if (allCoaches.Count == 0)
        {
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, allCoaches.Count);
        return allCoaches[randomIndex];
    }

    /// <summary>
    /// Transform database record to UI format
    /// </summary>
    private CoachProfileWrapper TransformToCoachProfile(CoachDatabaseRecord dbCoach)
    {
        var profile = new CoachProfileWrapper
        {
            name = dbCoach.coach_name,
            experience = dbCoach.experience,
            previousTeam = string.IsNullOrEmpty(dbCoach.prev_team) ? "Free Agent" : GetTeamNameFromId(dbCoach.prev_team),
            championshipWins = dbCoach.championship_won,
            specialties = TransformToSpecialties(dbCoach),
            contractTerms = TransformToContractTerms(dbCoach)
        };

        return profile;
    }

    /// <summary>
    /// Convert database stats to top 4 specialties
    /// </summary>
    private SpecialtyEntry[] TransformToSpecialties(CoachDatabaseRecord dbCoach)
    {
        List<SpecialtyEntry> allSpecialties = new List<SpecialtyEntry>();

        switch (dbCoach.coach_type.ToUpper())
        {
            case "D": // Defense Coach
                if (dbCoach.run_defence > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Run Defense", value = CalculateBonus(dbCoach.run_defence) });
                if (dbCoach.pressure_control > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Pressure Control", value = CalculateBonus(dbCoach.pressure_control) });
                if (dbCoach.coverage_discipline > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Coverage Discipline", value = CalculateBonus(dbCoach.coverage_discipline) });
                if (dbCoach.turnover > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Turnover Generation", value = CalculateBonus(dbCoach.turnover) });
                break;

            case "O": // Offense Coach
                if (dbCoach.passing_efficiency > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Passing Efficiency", value = CalculateBonus(dbCoach.passing_efficiency) });
                if (dbCoach.rush > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Rushing Attack", value = CalculateBonus(dbCoach.rush) });
                if (dbCoach.red_zone_conversion > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Red Zone Conversion", value = CalculateBonus(dbCoach.red_zone_conversion) });
                if (dbCoach.play_variation > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Play Variation", value = CalculateBonus(dbCoach.play_variation) });
                break;

            case "S": // Special Teams Coach
                if (dbCoach.field_goal_accuracy > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Field Goal Accuracy", value = CalculateBonus(dbCoach.field_goal_accuracy) });
                if (dbCoach.kickoff_instance > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Kickoff Distance", value = CalculateBonus(dbCoach.kickoff_instance) });
                if (dbCoach.return_speed > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Return Speed", value = CalculateBonus(dbCoach.return_speed) });
                if (dbCoach.return_coverage > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Return Coverage", value = CalculateBonus(dbCoach.return_coverage) });
                break;
        }

        // Sort by value and take top 4
        allSpecialties.Sort((a, b) => b.value.CompareTo(a.value));
        
        int count = Mathf.Min(4, allSpecialties.Count);
        SpecialtyEntry[] topSpecialties = new SpecialtyEntry[count];
        for (int i = 0; i < count; i++)
        {
            topSpecialties[i] = allSpecialties[i];
        }

        return topSpecialties;
    }

    /// <summary>
    /// Generate exactly 4 contract terms
    /// </summary>
    private ContractTermEntry[] TransformToContractTerms(CoachDatabaseRecord dbCoach)
    {
        float weeklySalary = (dbCoach.salary * 1000000f) / 52f;
        float performanceBonus = weeklySalary * 0.15f;
        float totalCost = weeklySalary * dbCoach.contract_length;

        return new ContractTermEntry[]
        {
            new ContractTermEntry { key = "Weekly Salary", value = $"${weeklySalary:N0}" },
            new ContractTermEntry { key = "Contract Length", value = $"{dbCoach.contract_length} games minimum" },
            new ContractTermEntry { key = "Performance Bonus", value = $"+${performanceBonus:N0} for 80%+ wins" },
            new ContractTermEntry { key = "Total Cost", value = $"${totalCost:N0}" }
        };
    }

    /// <summary>
    /// Calculate bonus percentage from database stat
    /// </summary>
    private int CalculateBonus(float statValue)
    {
        return Mathf.RoundToInt(Mathf.Clamp(statValue * 5f, 0f, 50f));
    }

    /// <summary>
    /// Create default coach when database fails
    /// </summary>
    private CoachProfileWrapper CreateDefaultCoach()
    {
        return new CoachProfileWrapper
        {
            name = "Default Coach",
            experience = 1,
            previousTeam = "None",
            championshipWins = 0,
            specialties = new SpecialtyEntry[] 
            {
                new SpecialtyEntry { key = "General Coaching", value = 15 }
            },
            contractTerms = new ContractTermEntry[]
            {
                new ContractTermEntry { key = "Weekly Salary", value = "$5,000" },
                new ContractTermEntry { key = "Contract Length", value = "4 games minimum" },
                new ContractTermEntry { key = "Performance Bonus", value = "+$750 for 80%+ wins" },
                new ContractTermEntry { key = "Total Cost", value = "$20,000" }
            }
        };
    }

    /// <summary>
    /// Populate UI with null safety
    /// </summary>
    private void PopulateUI()
    {
        // Set header texts with null checking
        if (coachNameText != null && coach != null)
            coachNameText.text = coach.name;

        // Set metadata texts with null checking
        if (experienceText != null && coach != null)
            experienceText.text = $"Experience: {coach.experience} years";
        if (previousTeamText != null && coach != null)
            previousTeamText.text = $"Previous Team: {coach.previousTeam}";
        if (championshipWinsText != null && coach != null)
            championshipWinsText.text = $"Championship Wins: {coach.championshipWins}";

        // Clear existing UI elements
        ClearUI();

        // Populate specialties with null checking
        if (coach?.specialties != null && specialityContainer != null && specialityRowPrefab != null)
        {
            foreach (var specialty in coach.specialties)
            {
                GameObject row = Instantiate(specialityRowPrefab, specialityContainer);
                TMP_Text keyText = row.transform.Find("Label")?.GetComponent<TMP_Text>();
                TMP_Text valueText = row.transform.Find("PercentText")?.GetComponent<TMP_Text>();
                RectTransform fillBar = row.transform.Find("ProgressBar")?.Find("ProgressBarFill")?.GetComponent<RectTransform>();

                if (keyText != null) keyText.text = specialty.key;
                if (valueText != null) valueText.text = specialty.value.ToString() + "%";

                if (fillBar != null)
                {
                    float maxValue = 80f;
                    float clampedPercent = Mathf.Clamp(specialty.value, 0, 100);
                    fillBar.sizeDelta = new Vector2(maxValue * (clampedPercent / 50f), fillBar.sizeDelta.y);
                }
            }
        }

        // Populate contract terms with null checking
        if (coach?.contractTerms != null && contractRowContainer != null && contractRowPrefab != null)
        {
            foreach (var term in coach.contractTerms)
            {
                GameObject row = Instantiate(contractRowPrefab, contractRowContainer);
                TMP_Text keyText = row.transform.Find("Label")?.GetComponent<TMP_Text>();
                TMP_Text valueText = row.transform.Find("Value")?.GetComponent<TMP_Text>();

                if (keyText != null) keyText.text = term.key;
                if (valueText != null) valueText.text = term.value;
            }
        }
    }

    /// <summary>
    /// Clear existing UI elements
    /// </summary>
    private void ClearUI()
    {
        if (specialityContainer != null)
        {
            foreach (Transform child in specialityContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (contractRowContainer != null)
        {
            foreach (Transform child in contractRowContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Testing controls for N and T keys
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (loadFromDatabase)
            {
                if (useAPI)
                {
                    StartCoroutine(LoadRandomCoachFromAPI());
                }
                else
                {
                    LoadRandomCoach();
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (loadFromDatabase)
            {
                if (useAPI)
                {
                    StartCoroutine(LoadNextCoachTypeFromAPI());
                }
                else
                {
                    LoadNextCoachType();
                }
            }
        }
        
        // F key to toggle between API and JSON
        if (Input.GetKeyDown(KeyCode.F))
        {
            useAPI = !useAPI;
            Debug.Log($"[CoachProfilePopulator] Switched to {(useAPI ? "API" : "JSON")} mode");
        }
    }

    /// <summary>
    /// Load random coach from API (N key)
    /// </summary>
    private IEnumerator LoadRandomCoachFromAPI()
    {
        Debug.Log("[CoachProfilePopulator] Loading random coach from API...");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/all"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    string wrappedJson = $"{{\"Items\":{jsonResponse}}}";
                    var wrapper = JsonUtility.FromJson<ApiCoachDetailsWrapper>(wrappedJson);

                    if (wrapper?.Items != null && wrapper.Items.Length > 0)
                    {
                        var selectedCoach = wrapper.Items[UnityEngine.Random.Range(0, wrapper.Items.Length)];
                        coach = TransformApiCoachToProfile(selectedCoach);
                        ClearUI();
                        PopulateUI();
                        Debug.Log($"[CoachProfilePopulator] Loaded random coach from API: {coach.name}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachProfilePopulator] Error loading random coach from API: {e.Message}");
                    LoadRandomCoach(); // Fallback to JSON
                }
            }
            else
            {
                Debug.LogError($"[CoachProfilePopulator] API request failed: {request.error}");
                LoadRandomCoach(); // Fallback to JSON
            }
        }
    }

    /// <summary>
    /// Load next coach type from API (T key)
    /// </summary>
    private IEnumerator LoadNextCoachTypeFromAPI()
    {
        Debug.Log("[CoachProfilePopulator] Loading next coach type from API...");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/all"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    string wrappedJson = $"{{\"Items\":{jsonResponse}}}";
                    var wrapper = JsonUtility.FromJson<ApiCoachDetailsWrapper>(wrappedJson);

                    if (wrapper?.Items != null && wrapper.Items.Length > 0)
                    {
                        // Determine next coach type
                        string[] types = { "D", "O", "S" };
                        string currentType = "D"; // Default to Defense
                        
                        // Cycle through types
                        int currentIndex = System.Array.IndexOf(types, currentType);
                        currentIndex = (currentIndex + 1) % types.Length;
                        currentType = types[currentIndex];

                        // Filter coaches by type
                        var filteredCoaches = System.Array.FindAll(wrapper.Items, c => c.coachType.Equals(currentType, StringComparison.OrdinalIgnoreCase));
                        
                        if (filteredCoaches.Length > 0)
                        {
                            var selectedCoach = filteredCoaches[UnityEngine.Random.Range(0, filteredCoaches.Length)];
                            coach = TransformApiCoachToProfile(selectedCoach);
                            ClearUI();
                            PopulateUI();
                            string typeName = GetCoachTypeName(currentType);
                            Debug.Log($"[CoachProfilePopulator] Loaded {typeName} coach from API: {coach.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"[CoachProfilePopulator] No coaches found for type {currentType}");
                            LoadNextCoachType(); // Fallback to JSON
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachProfilePopulator] Error loading coach type from API: {e.Message}");
                    LoadNextCoachType(); // Fallback to JSON
                }
            }
            else
            {
                Debug.LogError($"[CoachProfilePopulator] API request failed: {request.error}");
                LoadNextCoachType(); // Fallback to JSON
            }
        }
    }

    /// <summary>
    /// Load specific coach by ID from API
    /// </summary>
    private IEnumerator LoadCoachByIdFromAPI(string coachId)
    {
        Debug.Log($"[CoachProfilePopulator] Loading coach by ID from API: {coachId}");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/{coachId}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    var apiCoach = JsonUtility.FromJson<ApiCoachDetails>(jsonResponse);

                    if (apiCoach != null)
                    {
                        coach = TransformApiCoachToProfile(apiCoach);
                        ClearUI();
                        PopulateUI();
                        Debug.Log($"[CoachProfilePopulator] Loaded specific coach from API: {coach.name}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachProfilePopulator] Error loading specific coach from API: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[CoachProfilePopulator] API request failed: {request.error}");
            }
        }
    }

    /// <summary>
    /// Load random coach (N key)
    /// </summary>
    public void LoadRandomCoach()
    {
        if (!loadFromDatabase) return;

        try
        {
            var dbCoach = LoadRandomCoachFromDatabase();
            if (dbCoach != null)
            {
                coach = TransformToCoachProfile(dbCoach);
                ClearUI();
                PopulateUI();
                Debug.Log($"[CoachProfilePopulator] Loaded random coach: {coach.name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachProfilePopulator] Error loading random coach: {e.Message}");
        }
    }

    /// <summary>
    /// Load next coach type D→O→S→D (T key)
    /// </summary>
    public void LoadNextCoachType()
    {
        if (!loadFromDatabase) return;

        try
        {
            string[] types = { "D", "O", "S" };
            string currentType = "D"; // Default to Defense
            
            // Cycle through types
            int currentIndex = System.Array.IndexOf(types, currentType);
            currentIndex = (currentIndex + 1) % types.Length;
            currentType = types[currentIndex];

            var dbCoach = LoadCoachByType(currentType);
            if (dbCoach != null)
            {
                coach = TransformToCoachProfile(dbCoach);
                ClearUI();
                PopulateUI();
                string typeName = GetCoachTypeName(currentType);
                Debug.Log($"[CoachProfilePopulator] Loaded {typeName} coach: {coach.name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachProfilePopulator] Error loading coach type: {e.Message}");
        }
    }

    /// <summary>
    /// Load coach by specific type
    /// </summary>
    private CoachDatabaseRecord LoadCoachByType(string coachType)
    {
        var allCoaches = ExtractCoachesFromDatabase();
        var filteredCoaches = new List<CoachDatabaseRecord>();

        foreach (var coach in allCoaches)
        {
            if (coach.coach_type.Equals(coachType, StringComparison.OrdinalIgnoreCase))
            {
                filteredCoaches.Add(coach);
            }
        }

        if (filteredCoaches.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, filteredCoaches.Count);
        return filteredCoaches[randomIndex];
    }

    /// <summary>
    /// Get coach type display name
    /// </summary>
    private string GetCoachTypeName(string coachType)
    {
        switch (coachType.ToUpper())
        {
            case "D": return "Defense";
            case "O": return "Offense";
            case "S": return "Special Teams";
            default: return "Unknown";
        }
    }
}