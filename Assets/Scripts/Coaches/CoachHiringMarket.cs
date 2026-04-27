using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoachHiringMarket : MonoBehaviour
{
    [Header("Team Configuration")]
    [SerializeField] private string teamId = "4d1c8be1-c9f0-4f0f-9e91-b424d8343f86";

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

    [Header("Testing Controls")]
    public TextMeshProUGUI instructionsText;

    private string currentFilter = "ALL";
    private string[] filterTypes = { "ALL", "D", "O", "S" };
    private int currentFilterIndex = 0;

    private CoachDatabaseRecord dbCoach1;
    private CoachDatabaseRecord dbCoach2;

    private void Start()
    {
        if (instructionsText != null)
            instructionsText.text = "N = New Coaches, F = Filter Type";

        SetupFilterDropdown();
    }

    // OnEnable fires every time this screen is shown, not just once on first load.
    private void OnEnable()
    {
        RefreshCoaches();
        UpdateBudgetDisplay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
            RefreshCoaches();

        if (Input.GetKeyDown(KeyCode.F))
            CycleFilterType();
    }

    // Loads available coaches from CoachesService, applies current filter, picks 2 random.
    public void RefreshCoaches()
    {
        var available = CoachesService.GetAvailableCoaches(CoachesService.LocalPlayerId);
        var filtered = FilterCoaches(available, currentFilter);

        if (filtered.Count >= 2)
        {
            var random = GetRandomCoaches(filtered, 2);
            dbCoach1 = random[0];
            dbCoach2 = random[1];
        }
        else if (filtered.Count == 1)
        {
            dbCoach1 = filtered[0];
            dbCoach2 = null;
        }
        else
        {
            dbCoach1 = null;
            dbCoach2 = null;
        }

        UpdateCoachDisplay();
    }

    private void UpdateBudgetDisplay()
    {
        if (budgetText == null) return;
        var wallet = new EconomyService().GetWallet(CoachesService.LocalPlayerId);
        budgetText.text = wallet != null
            ? $"Budget: {wallet.coins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} coins"
            : "Budget: --";
    }

    #region Filter Management

    private void SetupFilterDropdown()
    {
        if (filterDropdown == null) return;
        filterDropdown.ClearOptions();
        filterDropdown.AddOptions(new List<string> { "All", "Defense", "Offense", "Special Teams" });
        filterDropdown.onValueChanged.AddListener(OnFilterChanged);
    }

    public void OnFilterChanged(int index)
    {
        string[] filterMappings = { "ALL", "D", "O", "S" };
        if (index < 0 || index >= filterMappings.Length) return;
        currentFilter = filterMappings[index];
        currentFilterIndex = index;
        RefreshCoaches();
        Debug.Log($"[CoachHiringMarket] Filter changed to: {GetFilterDisplayName(currentFilter)}");
    }

    public void ToggleFilter()
    {
        RefreshCoaches();
    }

    public void CycleFilterType()
    {
        currentFilterIndex = (currentFilterIndex + 1) % filterTypes.Length;
        currentFilter = filterTypes[currentFilterIndex];

        if (filterDropdown != null)
            filterDropdown.value = currentFilterIndex;

        RefreshCoaches();
        Debug.Log($"[CoachHiringMarket] Cycled filter to: {GetFilterDisplayName(currentFilter)}");
    }

    private string GetFilterDisplayName(string filter)
    {
        switch (filter)
        {
            case "ALL": return "All";
            case "D":   return "Defense";
            case "O":   return "Offense";
            case "S":   return "Special Teams";
            default:    return "Unknown";
        }
    }

    #endregion

    #region UI Update Methods

    private void UpdateCoachDisplay()
    {
        UpdateDatabaseCoachDisplay1(dbCoach1);
        UpdateDatabaseCoachDisplay2(dbCoach2);
    }

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
            ratingText1.text = "Rating: " + $"{CalculateStarRating(coach.overall_rating)} Stars";

        if (Type1 != null)
            Type1.text = "Type: " + GetCoachTypeDisplayName(coach.coach_type);

        UpdateSpecialtiesDisplay1(coach);
    }

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
            ratingText2.text = "Rating: " + $"{CalculateStarRating(coach.overall_rating)} Stars";

        if (Type2 != null)
            Type2.text = "Type: " + GetCoachTypeDisplayName(coach.coach_type);

        UpdateSpecialtiesDisplay2(coach);
    }

    private void UpdateSpecialtiesDisplay1(CoachDatabaseRecord coach)
    {
        if (specialtiesContainer1 == null || specialtyPrefab1 == null) return;
        foreach (Transform child in specialtiesContainer1)
            Destroy(child.gameObject);
        foreach (var specialty in GetTopSpecialties(coach, 3))
        {
            var obj = Instantiate(specialtyPrefab1, specialtiesContainer1);
            var text = obj.GetComponent<TextMeshProUGUI>();
            if (text != null) text.text = $"{specialty.key}: {specialty.value}%";
        }
    }

    private void UpdateSpecialtiesDisplay2(CoachDatabaseRecord coach)
    {
        if (specialtiesContainer2 == null || specialtyPrefab2 == null) return;
        foreach (Transform child in specialtiesContainer2)
            Destroy(child.gameObject);
        foreach (var specialty in GetTopSpecialties(coach, 3))
        {
            var obj = Instantiate(specialtyPrefab2, specialtiesContainer2);
            var text = obj.GetComponent<TextMeshProUGUI>();
            if (text != null) text.text = $"{specialty.key}: {specialty.value}%";
        }
    }

    #endregion

    #region Helper Methods

    private List<CoachDatabaseRecord> FilterCoaches(List<CoachDatabaseRecord> coaches, string filter)
    {
        if (filter == "ALL") return new List<CoachDatabaseRecord>(coaches);
        return coaches
            .Where(c => string.Equals(c.coach_type, filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private List<CoachDatabaseRecord> GetRandomCoaches(List<CoachDatabaseRecord> coaches, int count)
    {
        var result = new List<CoachDatabaseRecord>();
        var pool = new List<CoachDatabaseRecord>(coaches);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }

    private bool IsSlotOccupied(string coachType)
    {
        if (CoachManager.instance == null) return false;
        switch (coachType?.ToUpper())
        {
            case "O": return CoachManager.instance.offenseCoach != null;
            case "D": return CoachManager.instance.defenseCoach != null;
            case "S": return CoachManager.instance.SpecialCoach != null;
            default:  return false;
        }
    }

    private List<SpecialtyEntry> GetTopSpecialties(CoachDatabaseRecord coach, int count)
    {
        var specialties = new List<SpecialtyEntry>();

        switch (coach.coach_type?.ToUpper())
        {
            case "D":
                if (coach.run_defence > 0)         specialties.Add(new SpecialtyEntry { key = "Run Defense",         value = CalculateBonus(coach.run_defence) });
                if (coach.pressure_control > 0)    specialties.Add(new SpecialtyEntry { key = "Pressure Control",    value = CalculateBonus(coach.pressure_control) });
                if (coach.coverage_discipline > 0) specialties.Add(new SpecialtyEntry { key = "Coverage Discipline", value = CalculateBonus(coach.coverage_discipline) });
                if (coach.turnover > 0)            specialties.Add(new SpecialtyEntry { key = "Turnover Generation", value = CalculateBonus(coach.turnover) });
                break;
            case "O":
                if (coach.passing_efficiency > 0)  specialties.Add(new SpecialtyEntry { key = "Passing Efficiency",  value = CalculateBonus(coach.passing_efficiency) });
                if (coach.rush > 0)                specialties.Add(new SpecialtyEntry { key = "Rushing Attack",       value = CalculateBonus(coach.rush) });
                if (coach.red_zone_conversion > 0) specialties.Add(new SpecialtyEntry { key = "Red Zone Conversion", value = CalculateBonus(coach.red_zone_conversion) });
                if (coach.play_variation > 0)      specialties.Add(new SpecialtyEntry { key = "Play Variation",      value = CalculateBonus(coach.play_variation) });
                break;
            case "S":
                if (coach.field_goal_accuracy > 0) specialties.Add(new SpecialtyEntry { key = "Field Goal Accuracy", value = CalculateBonus(coach.field_goal_accuracy) });
                if (coach.kickoff_instance > 0)    specialties.Add(new SpecialtyEntry { key = "Kickoff Distance",    value = CalculateBonus(coach.kickoff_instance) });
                if (coach.return_speed > 0)        specialties.Add(new SpecialtyEntry { key = "Return Speed",        value = CalculateBonus(coach.return_speed) });
                if (coach.return_coverage > 0)     specialties.Add(new SpecialtyEntry { key = "Return Coverage",     value = CalculateBonus(coach.return_coverage) });
                break;
        }

        specialties.Sort((a, b) => b.value.CompareTo(a.value));
        return specialties.GetRange(0, Mathf.Min(count, specialties.Count));
    }

    private int CalculateBonus(float statValue) =>
        Mathf.RoundToInt(Mathf.Clamp(statValue * 5f, 0f, 50f));

    private int CalculateStarRating(float overallRating) =>
        Mathf.RoundToInt(Mathf.Clamp(overallRating, 1f, 5f));

    private string GetCoachTypeDisplayName(string coachType)
    {
        switch (coachType?.ToUpper())
        {
            case "D": return "Defense";
            case "O": return "Offense";
            case "S": return "Special Teams";
            default:  return "Unknown";
        }
    }

    #endregion

    #region Public Methods (Button Handlers)

    public void HireCoach1()
    {
        if (dbCoach1 == null || string.IsNullOrEmpty(dbCoach1.coach_id))
        {
            Debug.LogWarning("[CoachHiringMarket] No coach in slot 1 to hire.");
            return;
        }

        if (IsSlotOccupied(dbCoach1.coach_type))
        {
            Debug.LogWarning($"[CoachHiringMarket] A {GetCoachTypeDisplayName(dbCoach1.coach_type)} coach slot is already filled — fire the current coach first.");
            return;
        }

        if (CoachesService.TryHireCoach(teamId, dbCoach1.coach_id, out var hired, CoachesService.LocalPlayerId))
        {
            Debug.Log($"[CoachHiringMarket] Successfully hired: {hired.coach_name}");
            NotifyCoachHired(hired);
            UpdateBudgetDisplay();
        }
        else
        {
            Debug.LogWarning($"[CoachHiringMarket] Failed to hire {dbCoach1.coach_name} — insufficient funds or invalid coach.");
        }
    }

    public void HireCoach2()
    {
        if (dbCoach2 == null || string.IsNullOrEmpty(dbCoach2.coach_id))
        {
            Debug.LogWarning("[CoachHiringMarket] No coach in slot 2 to hire.");
            return;
        }

        if (IsSlotOccupied(dbCoach2.coach_type))
        {
            Debug.LogWarning($"[CoachHiringMarket] A {GetCoachTypeDisplayName(dbCoach2.coach_type)} coach slot is already filled — fire the current coach first.");
            return;
        }

        if (CoachesService.TryHireCoach(teamId, dbCoach2.coach_id, out var hired, CoachesService.LocalPlayerId))
        {
            Debug.Log($"[CoachHiringMarket] Successfully hired: {hired.coach_name}");
            NotifyCoachHired(hired);
            UpdateBudgetDisplay();
        }
        else
        {
            Debug.LogWarning($"[CoachHiringMarket] Failed to hire {dbCoach2.coach_name} — insufficient funds or invalid coach.");
        }
    }

    // Kept for legacy scene wiring compatibility.
    public void Initialize(CoachType type) { }

    #endregion

    // Syncs in-memory CoachManager state and fires OnCoachHired so CoachSlotUI updates.
    private void NotifyCoachHired(CoachDatabaseRecord coach)
    {
        if (CoachManager.instance == null) return;
        var coachData = CoachData.CreateFromDatabaseRecord(coach);
        CoachManager.instance.HireCoach(coachData);
    }
}
