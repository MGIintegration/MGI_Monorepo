using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.UI;

public class CoachManager : MonoBehaviour
{
    public static CoachManager instance;

    [Header("Available Coaches")]
    public List<CoachData> allCoaches = new List<CoachData>();

    [Header("Current Staff - Defense and Offense Only")]
    public CoachData defenseCoach;
    public CoachData offenseCoach;
    public CoachData SpecialCoach;

    public Button fireOffence;
    public Button fireDefense;

    [Header("API Configuration")]
    [SerializeField] private string baseURL = "http://localhost:5175";
    [SerializeField] private string teamId = "4d1c8be1-c9f0-4f0f-9e91-b424d8343f86"; // Default team ID
    [SerializeField] private bool loadFromAPI = true; // Toggle for API vs JSON fallback
    private bool isAPIAvailable = false;

    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy this one.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // This is the one and only instance.
        instance = this;
}


    // Events
    public static event Action<CoachData, CoachType> OnCoachHired;
    public static event Action<CoachType> OnCoachFired;

    private void Start()
    {
        fireOffence.onClick.AddListener(() =>FireCoach(CoachType.Offense));
        fireDefense.onClick.AddListener(() =>FireCoach(CoachType.Defense));

        InitializeSystem();
    }

    private void InitializeSystem()
    {
        Debug.Log($"[CoachManager] Initializing system. loadFromAPI = {loadFromAPI}");
        
        // Load coaches from Resources only if not using API
        if (!loadFromAPI)
        {
            LoadCoaches();
        }
        else
        {
            Debug.Log("[CoachManager] Skipping ScriptableObject loading - using API mode");
            allCoaches.Clear(); // Clear any existing coaches
        }
        
        // Pre-load 2 coaches from API/database if enabled
        if (loadFromAPI)
        {
            StartCoroutine(PreLoadTeamCoaches());
        }
    }

    private void LoadCoaches()
    {
        // Load from ScriptableObject Resources (static coaches)
        CoachData[] coaches = Resources.LoadAll<CoachData>("Coaches");
        allCoaches.Clear();
        allCoaches.AddRange(coaches);

        // Also load from database if available
        LoadCoachesFromDatabase();

        Debug.Log($"Loaded {allCoaches.Count} coaches total");
    }
    
    private void LoadCoachesFromDatabase()
    {
        string databasePath = Path.Combine(Application.streamingAssetsPath, "Database", "coach.json");
        
        if (!File.Exists(databasePath))
        {
            Debug.Log("No database file found, using only ScriptableObject coaches");
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(databasePath);
            JsonWrapper wrapper = JsonUtility.FromJson<JsonWrapper>("{\"Items\":" + jsonContent + "}");
            
            if (wrapper?.Items != null)
            {
                foreach (var dbRecord in wrapper.Items)
                {
                    // Convert database record to CoachData
                    CoachData coachData = CoachData.CreateFromDatabaseRecord(dbRecord);
                    allCoaches.Add(coachData);
                }
                Debug.Log($"Loaded {wrapper.Items.Length} coaches from database");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load coaches from database: {e.Message}");
        }
    }

    public bool HireCoach(CoachData coach)
    {
        if (coach == null || coach.isHired)
        {
            Debug.Log("Coach is null or already hired");
            return false;
        }
        // Only handle Defense and Offense for now
        if (coach.position != CoachType.Defense && coach.position != CoachType.Offense)
        {
            Debug.Log("Only Defense and Offense coaches supported currently");
            return false;
        }
        // Fire existing coach of same type if any
        if (coach.position == CoachType.Defense && defenseCoach != null)
        {
            FireCoach(CoachType.Defense);
        }
        else if (coach.position == CoachType.Offense && offenseCoach != null)
        {
            FireCoach(CoachType.Offense);
        }
        // Hire new coach
        coach.isHired = true;
        //coach.contractWeeksRemaining = 
        if (coach.position == CoachType.Defense)
        {
            defenseCoach = coach;
        }
        else if (coach.position == CoachType.Offense)
        {
            offenseCoach = coach;
        }
        OnCoachHired?.Invoke(coach, coach.position);
        Debug.Log($"Hired {coach.coachName} for {coach.position}");
        return true;
    }

    public bool FireCoach(CoachType position)
    {
        CoachData coachToFire = null;
        CoachSlotUI slotToUpdate = null;

        if (position == CoachType.Defense)
        {
            coachToFire = defenseCoach;
            defenseCoach = null;
        }
        else if (position == CoachType.Offense)
        {
            coachToFire = offenseCoach;
            offenseCoach = null;
        }

        if (coachToFire == null)
        {
            Debug.Log($"No coach to fire for {position}");
            return false;
        }

        // Reset coach status
        coachToFire.isHired = false;

        //coachToFire.weeksEmployed = 0;

        // Update UI
        if (slotToUpdate != null)
            slotToUpdate.UpdateDisplay(null);

        OnCoachFired?.Invoke(position);
        Debug.Log($"Fired {coachToFire.coachName} from {position}");
        return true;
    }

    public List<CoachData> GetAvailableCoaches()
    {
        List<CoachData> available = new List<CoachData>();

        foreach (CoachData coach in allCoaches)
        {
            if (!coach.isHired)
            {
                // Only show Defense and Offense coaches for now
                if (coach.position == CoachType.Defense || coach.position == CoachType.Offense)
                {
                    available.Add(coach);
                }
            }
        }
        return available;
    }

    public List<CoachData> GetAvailableCoachesByType(CoachType type)
    {
        List<CoachData> available = new List<CoachData>();

        foreach (CoachData coach in allCoaches)
        {
            if (!coach.isHired && coach.position == type)
            {
                // Only Defense and Offense for now
                if (type == CoachType.Defense || type == CoachType.Offense)
                {
                    available.Add(coach);
                }
            }
        }

        return available;
    }

    public CoachData GetCoachByType(CoachType type)
    {
        switch (type)
        {
            case CoachType.Defense:
                return defenseCoach;
            case CoachType.Offense:
                return offenseCoach;
            default:
                return null;
        }
    }

    public bool HasCoachForPosition(CoachType position)
    {
        return GetCoachByType(position) != null;
    }

    // Get current team bonuses (Defense and Offense only)
    public TeamBonus GetCurrentTeamBonus()
    {
        TeamBonus bonus = new TeamBonus();

        if (defenseCoach != null)
            bonus.defenseBonus = defenseCoach.GetEffectiveDefenseBonus();

        if (offenseCoach != null)
            bonus.offenseBonus = offenseCoach.GetEffectiveOffenseBonus();

        return bonus;
    }

    #region API Integration for Pre-Loading Coaches

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
    public class ApiCoachWrapper
    {
        public ApiCoach[] coaches;
    }

    /// <summary>
    /// Pre-load 2 coaches (1 offense, 1 defense) from API for the team
    /// </summary>
    private IEnumerator PreLoadTeamCoaches()
    {
        Debug.Log("[CoachManager] Starting pre-load of team coaches from API...");
        
        // Check if coaches are already loaded
        if (defenseCoach != null && offenseCoach != null)
        {
            Debug.Log("[CoachManager] Coaches already pre-loaded, skipping...");
            yield break;
        }
        
        // Test API connection first
        yield return StartCoroutine(TestAPIConnection());
        
        if (isAPIAvailable)
        {
            yield return StartCoroutine(LoadCoachesFromAPI());
        }
        else
        {
            // Fallback to JSON loading
            LoadCoachesFromJSON();
        }
    }

    /// <summary>
    /// Test if API is available
    /// </summary>
    private IEnumerator TestAPIConnection()
    {
        string url = $"{baseURL}/api/coach/all";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5; // 5 second timeout
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isAPIAvailable = true;
                Debug.Log("[CoachManager] API connection successful");
            }
            else
            {
                isAPIAvailable = false;
                Debug.LogWarning($"[CoachManager] API connection failed: {request.error}. Using JSON fallback.");
            }
        }
    }

    /// <summary>
    /// Load coaches from API and assign to team positions
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
                        // Filter coaches by type
                        var defenseCoaches = wrapper.coaches.Where(c => c.coachType == "D").ToList();
                        var offenseCoaches = wrapper.coaches.Where(c => c.coachType == "O").ToList();
                        
                        Debug.Log($"[CoachManager] Found {defenseCoaches.Count} defense coaches and {offenseCoaches.Count} offense coaches from API");
                        
                        // Select one coach of each type
                        if (defenseCoaches.Count > 0 && defenseCoach == null)
                        {
                            var selectedDefenseCoach = defenseCoaches[UnityEngine.Random.Range(0, defenseCoaches.Count)];
                            var defenseCoachData = ConvertApiCoachToCoachData(selectedDefenseCoach);
                            Debug.Log($"[CoachManager] Converting defense coach: {selectedDefenseCoach.coachName} -> {defenseCoachData.coachName}");
                            HireCoach(defenseCoachData);
                            Debug.Log($"[CoachManager] Pre-loaded defense coach: {defenseCoachData.coachName}");
                        }
                        
                        if (offenseCoaches.Count > 0 && offenseCoach == null)
                        {
                            var selectedOffenseCoach = offenseCoaches[UnityEngine.Random.Range(0, offenseCoaches.Count)];
                            var offenseCoachData = ConvertApiCoachToCoachData(selectedOffenseCoach);
                            Debug.Log($"[CoachManager] Converting offense coach: {selectedOffenseCoach.coachName} -> {offenseCoachData.coachName}");
                            HireCoach(offenseCoachData);
                            Debug.Log($"[CoachManager] Pre-loaded offense coach: {offenseCoachData.coachName}");
                        }
                        
                        Debug.Log("[CoachManager] Successfully pre-loaded coaches from API");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CoachManager] Failed to parse API response: {e.Message}");
                    LoadCoachesFromJSON(); // Fallback to JSON
                }
            }
            else
            {
                Debug.LogError($"[CoachManager] API request failed: {request.error}");
                LoadCoachesFromJSON(); // Fallback to JSON
            }
        }
    }

    /// <summary>
    /// Convert API coach to CoachData format
    /// </summary>
    private CoachData ConvertApiCoachToCoachData(ApiCoach apiCoach)
    {
        // First convert to database record format, then to CoachData
        var dbRecord = new CoachDatabaseRecord
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
        
        return CoachData.CreateFromDatabaseRecord(dbRecord);
    }

    /// <summary>
    /// Fallback method to load coaches from JSON
    /// </summary>
    private void LoadCoachesFromJSON()
    {
        Debug.Log("[CoachManager] Loading coaches from JSON fallback...");
        
        string databasePath = Path.Combine(Application.streamingAssetsPath, "Database", "coach.json");
        
        if (!File.Exists(databasePath))
        {
            Debug.LogWarning("[CoachManager] No JSON fallback available, using existing ScriptableObject coaches");
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(databasePath);
            JsonWrapper wrapper = JsonUtility.FromJson<JsonWrapper>("{\"Items\":" + jsonContent + "}");
            
            if (wrapper?.Items != null)
            {
                // Filter coaches by type
                var defenseCoaches = wrapper.Items.Where(c => c.coach_type == "D").ToList();
                var offenseCoaches = wrapper.Items.Where(c => c.coach_type == "O").ToList();
                
                Debug.Log($"[CoachManager] Found {defenseCoaches.Count} defense coaches and {offenseCoaches.Count} offense coaches from JSON");
                
                // Select one coach of each type
                if (defenseCoaches.Count > 0 && defenseCoach == null)
                {
                    var selectedDefenseCoach = defenseCoaches[UnityEngine.Random.Range(0, defenseCoaches.Count)];
                    var defenseCoachData = CoachData.CreateFromDatabaseRecord(selectedDefenseCoach);
                    Debug.Log($"[CoachManager] Converting JSON defense coach: {selectedDefenseCoach.coach_name} -> {defenseCoachData.coachName}");
                    HireCoach(defenseCoachData);
                    Debug.Log($"[CoachManager] Pre-loaded defense coach from JSON: {defenseCoachData.coachName}");
                }
                
                if (offenseCoaches.Count > 0 && offenseCoach == null)
                {
                    var selectedOffenseCoach = offenseCoaches[UnityEngine.Random.Range(0, offenseCoaches.Count)];
                    var offenseCoachData = CoachData.CreateFromDatabaseRecord(selectedOffenseCoach);
                    Debug.Log($"[CoachManager] Converting JSON offense coach: {selectedOffenseCoach.coach_name} -> {offenseCoachData.coachName}");
                    HireCoach(offenseCoachData);
                    Debug.Log($"[CoachManager] Pre-loaded offense coach from JSON: {offenseCoachData.coachName}");
                }
                
                Debug.Log("[CoachManager] Successfully pre-loaded coaches from JSON");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoachManager] Failed to load coaches from JSON: {e.Message}");
        }
    }

    #endregion
}

[System.Serializable]
public class TeamBonus
{
    public int offenseBonus;
    public int defenseBonus;
    public int specialTeamsBonus; // Keep for future use
    public int TotalBonus => offenseBonus + defenseBonus + specialTeamsBonus;
}
