using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;
using UnityEngine.Networking;

public class CoachHiringMarket : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string baseURL = "http://localhost:5175";
    [SerializeField] private string teamId = "4d1c8be1-c9f0-4f0f-9e91-b424d8343f86"; // Default team ID
    private bool isAPIAvailable = false; // Track API availability
    
    [Header("Database Integration")]
    [SerializeField] private bool loadFromDatabase = true;
    
    [Header("Current Coach Slots")]
    public CoachData assignedCoach1;
    public CoachData assignedCoach2;

    [Header("UI States")]
    public GameObject emptyState;  // The "Empty" GameObject
    public GameObject hiredState;  // The "Hired" GameObject with Name, Salary, Rating, etc.

    [Header("Coach Slot 1 Elements")]
    public TextMeshProUGUI nameText1;
    public TextMeshProUGUI salaryText1;
    public TextMeshProUGUI ratingText1;
    public TextMeshProUGUI Type1;
    public Button viewCoachButton1;
    public Transform specialtiesContainer1;
    public GameObject specialtyPrefab1;

    [Header("Coach Slot 2 Elements")]
    public TextMeshProUGUI nameText2;
    public TextMeshProUGUI salaryText2;
    public TextMeshProUGUI ratingText2;
    public TextMeshProUGUI Type2;
    public Button viewCoachButton2;
    public Transform specialtiesContainer2;
    public GameObject specialtyPrefab2;

    [Header("Coach Filtering")]
    public Dropdown filterDropdown;
    public TextMeshProUGUI budgetText;
    private float currentBudget = 500000f; // Will be loaded from API

    [Header("Testing Controls")]
    public TextMeshProUGUI instructionsText;

    // Database variables
    private static List<CoachDatabaseRecord> cachedCoaches = null;
    private static bool isCacheInitialized = false;
    private string currentFilter = "ALL";
    private string[] filterTypes = { "ALL", "D", "O", "S" };
    private int currentFilterIndex = 0;

    // Dynamic coach data from database
    private CoachDatabaseRecord dbCoach1;
    private CoachDatabaseRecord dbCoach2;

    // API Response Models
    [System.Serializable]
    public class ApiCoach
    {
        public string coachId;
        public string coachName;
        public string coachType;
        public int experience;
        public float salary;
        public float totalCost;
        public int contractLength;
        public float bonus;
        public float overallRating;
        public float winLossPercentage;
        public int championshipWon;
        
        // Defensive Stats
        public float coverageDiscipline;
        public float runDefence;
        public float turnover;
        public float pressureControl;
        
        // Offensive Stats
        public float passingEfficiency;
        public float rush;
        public float redZoneConversion;
        public float playVariation;
        
        // Special Teams Stats
        public float kickoffDistance;
        public float returnCoverage;
        public float fieldGoalAccuracy;
        public float returnSpeed;
        
        public string currentTeam;
        public string prevTeam;
    }

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

    [System.Serializable]
    public class CoachHireRequest
    {
        public string coachId;
        public string teamId;
    }

    [System.Serializable]
    public class CoachHireResponse
    {
        public string message;
        public ApiTeam team;
    }

    private CoachData currentCoach;
    private CoachType slotType;

    private void Start()
    {
        // Initialize UI
        if (instructionsText != null)
        {
            instructionsText.text = "N = New Coaches, T = Toggle Filter, F = Filter Type";
        }

        // Setup filter dropdown
        SetupFilterDropdown();

        // Load team budget and initial coaches from API
        if (loadFromDatabase)
        {
            // Test API connection first
            StartCoroutine(TestAPIConnection());
        }
        else
        {
            // Set fallback budget display
            if (budgetText != null)
            {
                budgetText.text = $"Budget: ${currentBudget:N0}";
            }
            // Use existing assigned coaches
            UpdateCoachDisplay();
        }
    }

    /// <summary>
    /// Test if the API server is running
    /// </summary>
    private IEnumerator TestAPIConnection()
    {
        Debug.Log("[CoachHiringMarket] Testing API connection...");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{baseURL}/api/coach/all"))
        {
            request.timeout = 3; // Short timeout for connection test
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[CoachHiringMarket] ✅ API server is available - using dynamic API data");
                isAPIAvailable = true;
                StartCoroutine(LoadTeamBudget());
                StartCoroutine(LoadCoachesFromAPI());
            }
            else
            {
                Debug.LogWarning("[CoachHiringMarket] ⚠️ API server not available - using local fallback data");
                isAPIAvailable = false;
                
                // Use fallback budget
                currentBudget = 50000000;
                if (budgetText != null)
                {
                    budgetText.text = $"Budget: ${currentBudget:N0}";
                }
                
                // Load local coaches
                LoadCoachesFromLocal();
            }
        }
    }

    private void Update()
    {
        // Testing controls
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (loadFromDatabase)
            {
                StartCoroutine(LoadCoachesFromAPI());
            }
            else
            {
                LoadNewCoaches();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (loadFromDatabase)
            {
                StartCoroutine(LoadCoachesFromAPI());
            }
            else
            {
                ToggleFilter();
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            CycleFilterType();
        }

        // Update displays if using database
        if (loadFromDatabase)
        {
            UpdateCoachDisplay();
        }
        else
        {
            // Legacy behavior for assigned coaches
            UpdateHiredStateDisplay1(assignedCoach1);
            UpdateHiredStateDisplay2(assignedCoach2);
        }
    }

    #region API Integration Methods

    /// <summary>
    /// Load team budget from API
    /// </summary>
    private IEnumerator LoadTeamBudget()
    {
        string url = $"{baseURL}/api/coach/teamDetails/{teamId}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ApiTeam team = JsonUtility.FromJson<ApiTeam>(request.downloadHandler.text);
                    currentBudget = team.budget;
                    
                    if (budgetText != null)
                    {
                        budgetText.text = $"Budget: ${currentBudget:N0}";
                    }
                    
                    Debug.Log($"[CoachHiringMarket] Loaded team budget: ${currentBudget:N0}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachHiringMarket] Failed to parse team data: {e.Message}");
                    // Use fallback budget
                    if (budgetText != null)
                    {
                        budgetText.text = $"Budget: ${currentBudget:N0}";
                    }
                }
            }
            else
            {
                Debug.LogError($"[CoachHiringMarket] Failed to load team budget: {request.error}");
                Debug.Log("[CoachHiringMarket] Using fallback budget");
                
                // Use fallback budget - keep the existing default budget
                currentBudget = currentBudget > 0 ? currentBudget : 50000000; // Default to 50M if not set
                
                if (budgetText != null)
                {
                    budgetText.text = $"Budget: ${currentBudget:N0}";
                }
            }
        }
    }

    /// <summary>
    /// Load coaches from API with filtering
    /// </summary>
    private IEnumerator LoadCoachesFromAPI()
    {
        string url = $"{baseURL}/api/coach/all";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Parse JSON array response
                    string jsonResponse = request.downloadHandler.text;
                    string wrappedJson = $"{{\"coaches\":{jsonResponse}}}";
                    ApiCoachWrapper wrapper = JsonUtility.FromJson<ApiCoachWrapper>(wrappedJson);
                    
                    if (wrapper?.coaches != null && wrapper.coaches.Length > 0)
                    {
                        var filteredCoaches = FilterApiCoaches(wrapper.coaches, currentFilter);
                        
                        if (filteredCoaches.Count >= 2)
                        {
                            // Select 2 random coaches
                            var randomCoaches = GetRandomApiCoaches(filteredCoaches, 2);
                            ConvertApiCoachToDbRecord(randomCoaches[0], ref dbCoach1);
                            ConvertApiCoachToDbRecord(randomCoaches[1], ref dbCoach2);
                        }
                        else if (filteredCoaches.Count == 1)
                        {
                            ConvertApiCoachToDbRecord(filteredCoaches[0], ref dbCoach1);
                            dbCoach2 = CreateDefaultCoachRecord();
                        }
                        else
                        {
                            dbCoach1 = CreateDefaultCoachRecord();
                            dbCoach2 = CreateDefaultCoachRecord();
                        }
                        
                        Debug.Log($"[CoachHiringMarket] Loaded coaches from API with filter: {GetFilterDisplayName(currentFilter)}");
                    }
                    else
                    {
                        Debug.LogWarning("[CoachHiringMarket] No coaches received from API");
                        dbCoach1 = CreateDefaultCoachRecord();
                        dbCoach2 = CreateDefaultCoachRecord();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachHiringMarket] Failed to parse API response: {e.Message}");
                    dbCoach1 = CreateDefaultCoachRecord();
                    dbCoach2 = CreateDefaultCoachRecord();
                }
            }
            else
            {
                Debug.LogError($"[CoachHiringMarket] API request failed: {request.error}");
                Debug.Log("[CoachHiringMarket] Falling back to local coach data");
                LoadCoachesFromLocal();
                yield break; // Exit early since we loaded local data
            }
        }
    }

    /// <summary>
    /// Fallback method to load coaches from local CoachManager when API is unavailable
    /// </summary>
    private void LoadCoachesFromLocal()
    {
        Debug.Log("[CoachHiringMarket] Loading coaches from local database");
        
        // Try to get coaches from CoachManager
        CoachManager coachManager = FindObjectOfType<CoachManager>();
        if (coachManager != null && coachManager.allCoaches != null && coachManager.allCoaches.Count > 0)
        {
            var availableCoaches = coachManager.allCoaches.Where(coach => coach != null).ToList();
            
            if (availableCoaches.Count > 0)
            {
                // Convert CoachData to CoachDatabaseRecord for filtering
                var coachRecords = new List<CoachDatabaseRecord>();
                foreach (var coachData in availableCoaches)
                {
                    var record = ConvertCoachDataToDbRecord(coachData);
                    coachRecords.Add(record);
                }
                
                // Filter coaches based on current filter
                var filteredCoaches = FilterCoachesByType(coachRecords, ConvertStringFilterToInt(currentFilter));
                
                if (filteredCoaches.Count >= 2)
                {
                    dbCoach1 = filteredCoaches[0];
                    dbCoach2 = filteredCoaches[1];
                }
                else if (filteredCoaches.Count == 1)
                {
                    dbCoach1 = filteredCoaches[0];
                    dbCoach2 = CreateDefaultCoachRecord();
                }
                else
                {
                    // No coaches match filter, show first two available
                    dbCoach1 = coachRecords[0];
                    dbCoach2 = coachRecords.Count > 1 ? coachRecords[1] : CreateDefaultCoachRecord();
                }
                
                Debug.Log($"[CoachHiringMarket] Loaded {filteredCoaches.Count} coaches from local database");
                return;
            }
        }
        
        // Ultimate fallback - create default coaches
        Debug.LogWarning("[CoachHiringMarket] No local coaches available, using defaults");
        dbCoach1 = CreateDefaultCoachRecord();
        dbCoach2 = CreateDefaultCoachRecord();
    }

    /// <summary>
    /// Convert CoachData to CoachDatabaseRecord for compatibility
    /// </summary>
    private CoachDatabaseRecord ConvertCoachDataToDbRecord(CoachData coachData)
    {
        return new CoachDatabaseRecord
        {
            coach_id = coachData.coach_id ?? Guid.NewGuid().ToString(),
            coach_name = coachData.coachName ?? "Unknown Coach",
            coach_type = ConvertCoachTypeToString(coachData.position),
            experience = coachData.experience,
            championship_won = coachData.championship_won,
            overall_rating = coachData.overall_rating,
            salary = coachData.weeklySalary,
            contract_length = coachData.contract_length,
            current_team = coachData.current_team ?? "",
            prev_team = coachData.prev_team ?? "",
            
            // Stats
            run_defence = coachData.run_defence,
            pressure_control = coachData.pressure_control,
            coverage_discipline = coachData.coverage_discipline,
            turnover = coachData.turnover,
            passing_efficiency = coachData.passing_efficiency,
            rush = coachData.rush,
            red_zone_conversion = coachData.red_zone_conversion,
            play_variation = coachData.play_variation,
            field_goal_accuracy = coachData.field_goal_accuracy,
            kickoff_instance = coachData.kickoff_instance,
            return_speed = coachData.return_speed,
            return_coverage = coachData.return_coverage
        };
    }

    /// <summary>
    /// Convert CoachType enum to string
    /// </summary>
    private string ConvertCoachTypeToString(CoachType coachType)
    {
        switch (coachType)
        {
            case CoachType.Defense: return "D";
            case CoachType.Offense: return "O";
            case CoachType.SpecialTeams: return "S";
            default: return "O";
        }
    }

    /// <summary>
    /// Convert string filter to int filter for FilterCoachesByType method
    /// </summary>
    private int ConvertStringFilterToInt(string filterString)
    {
        switch (filterString?.ToUpper())
        {
            case "ALL": return 0;
            case "D": return 1;
            case "O": return 2;
            case "HEAD": return 3;
            case "ASSISTANT": return 4;
            default: return 0; // Default to "All"
        }
    }

    /// <summary>
    /// Filter local coaches by type to match API behavior
    /// </summary>
    private List<CoachDatabaseRecord> FilterCoachesByType(List<CoachDatabaseRecord> coaches, int filterType)
    {
        var filtered = new List<CoachDatabaseRecord>();
        
        foreach (var coach in coaches)
        {
            bool matches = false;
            switch (filterType)
            {
                case 0: // All
                    matches = true;
                    break;
                case 1: // Defense  
                    matches = coach.coach_type != null && 
                             (coach.coach_type.ToUpper().Contains("D") || 
                              coach.coach_type.ToLower().Contains("defense") || 
                              coach.coach_type.ToLower().Contains("defensive"));
                    break;
                case 2: // Offense
                    matches = coach.coach_type != null && 
                             (coach.coach_type.ToUpper().Contains("O") ||
                              coach.coach_type.ToLower().Contains("offense") || 
                              coach.coach_type.ToLower().Contains("offensive") ||
                              coach.coach_type.ToLower().Contains("attack"));
                    break;
                case 3: // Head Coach
                    matches = coach.coach_type != null && 
                             (coach.coach_type.ToLower().Contains("head") || 
                              coach.coach_type.ToLower().Contains("main"));
                    break;
                case 4: // Assistant
                    matches = coach.coach_type != null && 
                             coach.coach_type.ToLower().Contains("assistant");
                    break;
            }
            
            if (matches)
            {
                filtered.Add(coach);
            }
        }
        
        return filtered;
    }

    /// <summary>
    /// Check if coach is affordable
    /// </summary>
    private IEnumerator CheckCoachAffordability(string coachId, System.Action<bool> callback)
    {
        string url = $"{baseURL}/api/coach/isAffordable";
        
        CoachHireRequest requestData = new CoachHireRequest
        {
            coachId = coachId,
            teamId = teamId
        };
        
        string jsonData = JsonUtility.ToJson(requestData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    bool isAffordable = bool.Parse(request.downloadHandler.text);
                    callback(isAffordable);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachHiringMarket] Failed to parse affordability response: {e.Message}");
                    callback(false);
                }
            }
            else
            {
                Debug.LogError($"[CoachHiringMarket] Affordability check failed: {request.error}");
                callback(false);
            }
        }
    }

    /// <summary>
    /// Hire coach through API
    /// </summary>
    private IEnumerator HireCoachAPI(string coachId, System.Action<bool, string> callback)
    {
        string url = $"{baseURL}/api/coach/hire";
        
        CoachHireRequest requestData = new CoachHireRequest
        {
            coachId = coachId,
            teamId = teamId
        };
        
        string jsonData = JsonUtility.ToJson(requestData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    CoachHireResponse response = JsonUtility.FromJson<CoachHireResponse>(request.downloadHandler.text);
                    // Update budget after successful hire
                    if (response.team != null)
                    {
                        currentBudget = response.team.budget;
                        if (budgetText != null)
                        {
                            budgetText.text = $"Budget: ${currentBudget:N0}";
                        }
                    }
                    callback(true, response.message);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachHiringMarket] Failed to parse hire response: {e.Message}");
                    callback(false, "Failed to parse response");
                }
            }
            else
            {
                Debug.LogError($"[CoachHiringMarket] Coach hire failed: {request.error}");
                callback(false, request.error);
            }
        }
    }

    /// <summary>
    /// Helper class for parsing JSON array
    /// </summary>
    [System.Serializable]
    private class ApiCoachWrapper
    {
        public ApiCoach[] coaches;
    }

    /// <summary>
    /// Filter API coaches by type
    /// </summary>
    private List<ApiCoach> FilterApiCoaches(ApiCoach[] coaches, string filter)
    {
        if (filter == "ALL")
        {
            return new List<ApiCoach>(coaches);
        }
        
        return coaches.Where(c => c.coachType == filter).ToList();
    }

    /// <summary>
    /// Get random API coaches
    /// </summary>
    private List<ApiCoach> GetRandomApiCoaches(List<ApiCoach> coaches, int count)
    {
        var shuffled = coaches.OrderBy(c => UnityEngine.Random.value).ToList();
        return shuffled.Take(count).ToList();
    }

    /// <summary>
    /// Convert API coach to database record format
    /// </summary>
    private void ConvertApiCoachToDbRecord(ApiCoach apiCoach, ref CoachDatabaseRecord dbRecord)
    {
        dbRecord = new CoachDatabaseRecord
        {
            coach_id = apiCoach.coachId,
            coach_name = apiCoach.coachName,
            coach_type = apiCoach.coachType,
            experience = apiCoach.experience,
            salary = apiCoach.salary,
            contract_length = apiCoach.contractLength,
            overall_rating = apiCoach.overallRating,
            championship_won = apiCoach.championshipWon,
            
            // Defensive stats
            coverage_discipline = apiCoach.coverageDiscipline,
            run_defence = apiCoach.runDefence,
            turnover = apiCoach.turnover,
            pressure_control = apiCoach.pressureControl,
            
            // Offensive stats
            passing_efficiency = apiCoach.passingEfficiency,
            rush = apiCoach.rush,
            red_zone_conversion = apiCoach.redZoneConversion,
            play_variation = apiCoach.playVariation,
            
            // Special teams stats
            kickoff_instance = apiCoach.kickoffDistance,
            return_coverage = apiCoach.returnCoverage,
            field_goal_accuracy = apiCoach.fieldGoalAccuracy,
            return_speed = apiCoach.returnSpeed,
            
            current_team = apiCoach.currentTeam,
            prev_team = apiCoach.prevTeam
        };
    }

    #endregion

    #region Database Loading Methods (Legacy)

    /// <summary>
    /// Load coaches from database with filtering
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
                Debug.LogError($"[CoachHiringMarket] Coach JSON file not found at: {jsonPath}");
                return new List<CoachDatabaseRecord>();
            }

            string jsonContent = File.ReadAllText(jsonPath);
            string wrappedJson = $"{{\"Items\":{jsonContent}}}";
            var wrapper = JsonUtility.FromJson<JsonWrapper>(wrappedJson);
            
            if (wrapper == null || wrapper.Items == null)
            {
                Debug.LogError("[CoachHiringMarket] Failed to parse JSON");
                return new List<CoachDatabaseRecord>();
            }
            
            cachedCoaches = new List<CoachDatabaseRecord>(wrapper.Items);
            isCacheInitialized = true;
            
            Debug.Log($"[CoachHiringMarket] Successfully loaded {wrapper.Items.Length} coaches from JSON");
            return cachedCoaches;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachHiringMarket] Failed to load from JSON: {e.Message}");
            return new List<CoachDatabaseRecord>();
        }
    }

    /// <summary>
    /// Load new coaches based on current filter
    /// </summary>
    public void LoadNewCoaches()
    {
        if (!loadFromDatabase) return;

        try
        {
            var allCoaches = ExtractCoachesFromDatabase();
            var filteredCoaches = FilterCoaches(allCoaches, currentFilter);

            if (filteredCoaches.Count >= 2)
            {
                // Load two random coaches
                var randomCoaches = GetRandomCoaches(filteredCoaches, 2);
                dbCoach1 = randomCoaches[0];
                dbCoach2 = randomCoaches[1];

                Debug.Log($"[CoachHiringMarket] Loaded 2 new coaches with filter: {GetFilterDisplayName(currentFilter)}");
            }
            else if (filteredCoaches.Count == 1)
            {
                // Load one coach and a default
                dbCoach1 = filteredCoaches[0];
                dbCoach2 = CreateDefaultCoachRecord();
                Debug.Log($"[CoachHiringMarket] Loaded 1 coach + default with filter: {GetFilterDisplayName(currentFilter)}");
            }
            else
            {
                // No coaches found, load defaults
                dbCoach1 = CreateDefaultCoachRecord();
                dbCoach2 = CreateDefaultCoachRecord();
                Debug.LogWarning($"[CoachHiringMarket] No coaches found for filter: {GetFilterDisplayName(currentFilter)}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachHiringMarket] Error loading new coaches: {e.Message}");
        }
    }

    /// <summary>
    /// Filter coaches by type
    /// </summary>
    private List<CoachDatabaseRecord> FilterCoaches(List<CoachDatabaseRecord> allCoaches, string filter)
    {
        if (filter == "ALL")
        {
            return new List<CoachDatabaseRecord>(allCoaches);
        }

        var filtered = new List<CoachDatabaseRecord>();
        foreach (var coach in allCoaches)
        {
            if (coach.coach_type.Equals(filter, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(coach);
            }
        }
        return filtered;
    }

    /// <summary>
    /// Get random coaches from filtered list
    /// </summary>
    private List<CoachDatabaseRecord> GetRandomCoaches(List<CoachDatabaseRecord> coaches, int count)
    {
        var result = new List<CoachDatabaseRecord>();
        var availableCoaches = new List<CoachDatabaseRecord>(coaches);

        for (int i = 0; i < count && availableCoaches.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableCoaches.Count);
            result.Add(availableCoaches[randomIndex]);
            availableCoaches.RemoveAt(randomIndex);
        }

        return result;
    }

    #endregion

    #region Filter Management

    /// <summary>
    /// Setup filter dropdown
    /// </summary>
    private void SetupFilterDropdown()
    {
        if (filterDropdown != null)
        {
            filterDropdown.ClearOptions();
            var options = new List<string> { "All", "Defense", "Offense", "Special Teams" };
            filterDropdown.AddOptions(options);
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }
    }

    /// <summary>
    /// Handle filter dropdown change
    /// </summary>
    public void OnFilterChanged(int index)
    {
        string[] filterMappings = { "ALL", "D", "O", "S" };
        if (index >= 0 && index < filterMappings.Length)
        {
            currentFilter = filterMappings[index];
            currentFilterIndex = index;
            
            if (loadFromDatabase)
            {
                StartCoroutine(LoadCoachesFromAPI());
            }
            else
            {
                LoadNewCoaches();
            }
            
            Debug.Log($"[CoachHiringMarket] Filter changed to: {GetFilterDisplayName(currentFilter)}");
        }
    }

    /// <summary>
    /// Toggle filter via T key
    /// </summary>
    public void ToggleFilter()
    {
        if (loadFromDatabase)
        {
            StartCoroutine(LoadCoachesFromAPI());
        }
        else
        {
            LoadNewCoaches();
        }
        Debug.Log("[CoachHiringMarket] Refreshed coaches");
    }

    /// <summary>
    /// Cycle filter type via F key
    /// </summary>
    public void CycleFilterType()
    {
        currentFilterIndex = (currentFilterIndex + 1) % filterTypes.Length;
        currentFilter = filterTypes[currentFilterIndex];
        
        if (filterDropdown != null)
        {
            filterDropdown.value = currentFilterIndex;
        }
        
        if (loadFromDatabase)
        {
            StartCoroutine(LoadCoachesFromAPI());
        }
        else
        {
            LoadNewCoaches();
        }
        
        Debug.Log($"[CoachHiringMarket] Cycled to filter: {GetFilterDisplayName(currentFilter)}");
    }

    /// <summary>
    /// Get display name for filter
    /// </summary>
    private string GetFilterDisplayName(string filter)
    {
        switch (filter)
        {
            case "ALL": return "All";
            case "D": return "Defense";
            case "O": return "Offense";
            case "S": return "Special Teams";
            default: return "Unknown";
        }
    }

    #endregion



    #region UI Update Methods

    /// <summary>
    /// Update coach displays based on data source
    /// </summary>
    private void UpdateCoachDisplay()
    {
        if (loadFromDatabase)
        {
            UpdateDatabaseCoachDisplay1(dbCoach1);
            UpdateDatabaseCoachDisplay2(dbCoach2);
        }
        else
        {
            UpdateHiredStateDisplay1(assignedCoach1);
            UpdateHiredStateDisplay2(assignedCoach2);
        }
    }

    /// <summary>
    /// Update coach slot 1 with database data
    /// </summary>
    private void UpdateDatabaseCoachDisplay1(CoachDatabaseRecord coach)
    {
        if (coach == null) return;

        if (nameText1 != null)
            nameText1.text = "Name: " + coach.coach_name;

        if (salaryText1 != null)
        {
            float weeklySalary = (coach.salary * 1000000f) / 52f;
            salaryText1.text = "Salary: " + $"${weeklySalary:N0}/wk";
        }

        if (ratingText1 != null)
        {
            int starRating = CalculateStarRating(coach.overall_rating);
            ratingText1.text = "Rating: " + $"{starRating} Stars";
        }

        if (Type1 != null)
            Type1.text = "Type: " + GetCoachTypeDisplayName(coach.coach_type);

        // Update top 3 specialties
        UpdateSpecialtiesDisplay1(coach);
    }

    /// <summary>
    /// Update coach slot 2 with database data
    /// </summary>
    private void UpdateDatabaseCoachDisplay2(CoachDatabaseRecord coach)
    {
        if (coach == null) return;

        if (nameText2 != null)
            nameText2.text = "Name: " + coach.coach_name;

        if (salaryText2 != null)
        {
            float weeklySalary = (coach.salary * 1000000f) / 52f;
            salaryText2.text = "Salary: " + $"${weeklySalary:N0}/wk";
        }

        if (ratingText2 != null)
        {
            int starRating = CalculateStarRating(coach.overall_rating);
            ratingText2.text = "Rating: " + $"{starRating} Stars";
        }

        if (Type2 != null)
            Type2.text = "Type: " + GetCoachTypeDisplayName(coach.coach_type);

        // Update top 3 specialties
        UpdateSpecialtiesDisplay2(coach);
    }

    /// <summary>
    /// Update top 3 specialties for coach slot 1
    /// </summary>
    private void UpdateSpecialtiesDisplay1(CoachDatabaseRecord coach)
    {
        if (specialtiesContainer1 == null || specialtyPrefab1 == null) return;

        // Clear existing specialties
        foreach (Transform child in specialtiesContainer1)
        {
            Destroy(child.gameObject);
        }

        // Get top 3 specialties
        var specialties = GetTopSpecialties(coach, 3);

        // Create specialty UI elements
        foreach (var specialty in specialties)
        {
            GameObject specialtyObj = Instantiate(specialtyPrefab1, specialtiesContainer1);
            TextMeshProUGUI specialtyText = specialtyObj.GetComponent<TextMeshProUGUI>();
            if (specialtyText != null)
            {
                specialtyText.text = $"{specialty.key}: {specialty.value}%";
            }
        }
    }

    /// <summary>
    /// Update top 3 specialties for coach slot 2
    /// </summary>
    private void UpdateSpecialtiesDisplay2(CoachDatabaseRecord coach)
    {
        if (specialtiesContainer2 == null || specialtyPrefab2 == null) return;

        // Clear existing specialties
        foreach (Transform child in specialtiesContainer2)
        {
            Destroy(child.gameObject);
        }

        // Get top 3 specialties
        var specialties = GetTopSpecialties(coach, 3);

        // Create specialty UI elements
        foreach (var specialty in specialties)
        {
            GameObject specialtyObj = Instantiate(specialtyPrefab2, specialtiesContainer2);
            TextMeshProUGUI specialtyText = specialtyObj.GetComponent<TextMeshProUGUI>();
            if (specialtyText != null)
            {
                specialtyText.text = $"{specialty.key}: {specialty.value}%";
            }
        }
    }

    /// <summary>
    /// Legacy method for assigned coach data
    /// </summary>
    private void UpdateHiredStateDisplay1(CoachData coach)
    {
        if (coach == null) return;

        if (nameText1 != null)
            nameText1.text = "Name: " + coach.coachName;

        if (salaryText1 != null)
            salaryText1.text = "Salary: " + $"${coach.weeklySalary:N0}/wk";

        if (ratingText1 != null)
            ratingText1.text = "Rating: " + $"{coach.starRating} Stars";

        if (Type1 != null)
            Type1.text = "Type: " + $"{coach.type}";
    }

    /// <summary>
    /// Legacy method for assigned coach data
    /// </summary>
    private void UpdateHiredStateDisplay2(CoachData coach)
    {
        if (coach == null) return;

        if (nameText2 != null)
            nameText2.text = "Name: " + coach.coachName;

        if (salaryText2 != null)
            salaryText2.text = "Salary: " + $"${coach.weeklySalary:N0}/wk";

        if (ratingText2 != null)
            ratingText2.text = "Rating: " + $"{coach.starRating} Stars";

        if (Type2 != null)
            Type2.text = "Type: " + $"{coach.type}";
    }

    #endregion


    #region Helper Methods

    /// <summary>
    /// Get top N specialties for a coach
    /// </summary>
    private List<SpecialtyEntry> GetTopSpecialties(CoachDatabaseRecord coach, int count)
    {
        List<SpecialtyEntry> allSpecialties = new List<SpecialtyEntry>();

        switch (coach.coach_type.ToUpper())
        {
            case "D": // Defense Coach
                if (coach.run_defence > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Run Defense", value = CalculateBonus(coach.run_defence) });
                if (coach.pressure_control > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Pressure Control", value = CalculateBonus(coach.pressure_control) });
                if (coach.coverage_discipline > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Coverage Discipline", value = CalculateBonus(coach.coverage_discipline) });
                if (coach.turnover > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Turnover Generation", value = CalculateBonus(coach.turnover) });
                break;

            case "O": // Offense Coach
                if (coach.passing_efficiency > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Passing Efficiency", value = CalculateBonus(coach.passing_efficiency) });
                if (coach.rush > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Rushing Attack", value = CalculateBonus(coach.rush) });
                if (coach.red_zone_conversion > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Red Zone Conversion", value = CalculateBonus(coach.red_zone_conversion) });
                if (coach.play_variation > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Play Variation", value = CalculateBonus(coach.play_variation) });
                break;

            case "S": // Special Teams Coach
                if (coach.field_goal_accuracy > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Field Goal Accuracy", value = CalculateBonus(coach.field_goal_accuracy) });
                if (coach.kickoff_instance > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Kickoff Distance", value = CalculateBonus(coach.kickoff_instance) });
                if (coach.return_speed > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Return Speed", value = CalculateBonus(coach.return_speed) });
                if (coach.return_coverage > 0)
                    allSpecialties.Add(new SpecialtyEntry { key = "Return Coverage", value = CalculateBonus(coach.return_coverage) });
                break;
        }

        // Sort by value and take top N
        allSpecialties.Sort((a, b) => b.value.CompareTo(a.value));
        
        int actualCount = Mathf.Min(count, allSpecialties.Count);
        return allSpecialties.GetRange(0, actualCount);
    }

    /// <summary>
    /// Calculate percentage bonus from database stat
    /// </summary>
    private int CalculateBonus(float statValue)
    {
        return Mathf.RoundToInt(Mathf.Clamp(statValue * 5f, 0f, 50f));
    }

    /// <summary>
    /// Calculate star rating from overall rating
    /// </summary>
    private int CalculateStarRating(float overallRating)
    {
        return Mathf.RoundToInt(Mathf.Clamp(overallRating, 1f, 5f));
    }

    /// <summary>
    /// Get coach type display name
    /// </summary>
    private string GetCoachTypeDisplayName(string coachType)
    {
        switch (coachType?.ToUpper())
        {
            case "D": return "Defense";
            case "O": return "Offense";
            case "S": return "Special Teams";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Create default coach record when none available
    /// </summary>
    private CoachDatabaseRecord CreateDefaultCoachRecord()
    {
        return new CoachDatabaseRecord
        {
            coach_name = "Available Slot",
            coach_type = "D",
            experience = 1,
            salary = 0.1f,
            overall_rating = 2.0f,
            run_defence = 3.0f,
            pressure_control = 2.5f,
            coverage_discipline = 2.0f,
            turnover = 2.5f
        };
    }

    #endregion

    #region Public Methods (for buttons)

    /// <summary>
    /// Hire coach from slot 1
    /// </summary>
    public void HireCoach1() 
    {
        if (loadFromDatabase && dbCoach1 != null && !string.IsNullOrEmpty(dbCoach1.coach_id))
        {
            // Check affordability first
            StartCoroutine(CheckCoachAffordability(dbCoach1.coach_id, (isAffordable) =>
            {
                if (isAffordable)
                {
                    StartCoroutine(HireCoachAPI(dbCoach1.coach_id, (success, message) =>
                    {
                        if (success)
                        {
                            Debug.Log($"[CoachHiringMarket] Successfully hired coach: {dbCoach1.coach_name}");
                            // Refresh coaches after hire
                            StartCoroutine(LoadCoachesFromAPI());
                        }
                        else
                        {
                            Debug.LogError($"[CoachHiringMarket] Failed to hire coach: {message}");
                        }
                    }));
                }
                else
                {
                    Debug.LogWarning($"[CoachHiringMarket] Cannot afford coach: {dbCoach1.coach_name}");
                }
            }));
        }
        else if (assignedCoach1 != null)
        {
            CoachManager.instance.HireCoach(assignedCoach1);
        }
    }

    /// <summary>
    /// Hire coach from slot 2
    /// </summary>
    public void HireCoach2()
    {
        if (loadFromDatabase && dbCoach2 != null && !string.IsNullOrEmpty(dbCoach2.coach_id))
        {
            // Check affordability first
            StartCoroutine(CheckCoachAffordability(dbCoach2.coach_id, (isAffordable) =>
            {
                if (isAffordable)
                {
                    StartCoroutine(HireCoachAPI(dbCoach2.coach_id, (success, message) =>
                    {
                        if (success)
                        {
                            Debug.Log($"[CoachHiringMarket] Successfully hired coach: {dbCoach2.coach_name}");
                            // Refresh coaches after hire
                            StartCoroutine(LoadCoachesFromAPI());
                        }
                        else
                        {
                            Debug.LogError($"[CoachHiringMarket] Failed to hire coach: {message}");
                        }
                    }));
                }
                else
                {
                    Debug.LogWarning($"[CoachHiringMarket] Cannot afford coach: {dbCoach2.coach_name}");
                }
            }));
        }
        else if (assignedCoach2 != null)
        {
            CoachManager.instance.HireCoach(assignedCoach2);
        }
    }

    /// <summary>
    /// Initialize slot type (legacy compatibility)
    /// </summary>
    public void Initialize(CoachType type)
    {
        slotType = type;
    }

    #endregion
}
