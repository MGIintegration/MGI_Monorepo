using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoachSlotUI : MonoBehaviour
{
    public CoachData assignedCoach;
    public CoachType type;

    [Header("UI States")]
    public GameObject emptyState;  // The "Empty" GameObject
    public GameObject hiredState;  // The "Hired" GameObject with Name, Salary, Rating, etc.

    [Header("Hired State Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI salaryText;
    public TextMeshProUGUI ratingText;
    public TextMeshProUGUI DEFText;
    public TextMeshProUGUI OFFText;
    public Button viewCoachButton;
    public Button fireCoachButton;

    [Header("Empty State Elements")]
    public Button hireCoachButton;

    private CoachData currentCoach;
    private CoachType slotType;
   // private CoachManager coachManager;

    private void Start()
    {
        // Subscribe to coach events
        CoachManager.OnCoachHired += OnCoachHired;
        CoachManager.OnCoachFired += OnCoachFired;
        
        // Initialize display based on type
        Initialize(type);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        CoachManager.OnCoachHired -= OnCoachHired;
        CoachManager.OnCoachFired -= OnCoachFired;
    }
    
    private void OnCoachHired(CoachData coach, CoachType coachType)
    {
        // Update display if this slot matches the hired coach type
        if (coachType == type)
        {
            UpdateDisplay(coach);
        }
    }
    
    private void OnCoachFired(CoachType coachType)
    {
        // Update display if this slot matches the fired coach type
        if (coachType == type)
        {
            UpdateDisplay(null);
        }
    }
    private void Update()
    {
        // Check if the assigned coach has changed from CoachManager
        var currentManagerCoach = GetCurrentCoachFromManager();
        if (currentManagerCoach != assignedCoach)
        {
            assignedCoach = currentManagerCoach;
            UpdateDisplay(assignedCoach);
        }
    }
    
    private CoachData GetCurrentCoachFromManager()
    {
        if (CoachManager.instance == null) return null;
        
        switch (type)
        {
            case CoachType.Offense:
                return CoachManager.instance.offenseCoach;
            case CoachType.Defense:
                return CoachManager.instance.defenseCoach;
            default:
                return null;
        }
    }

    public void Initialize(CoachType type)
    {
        slotType = type;
        UpdateDisplay(null); // Start with empty state
        
    }

    public void UpdateDisplay(CoachData coach)
    {
        currentCoach = coach;
        
        if (coach == null)
        {
            // Show empty state
            if (emptyState != null) emptyState.SetActive(true);
            if (hiredState != null) hiredState.SetActive(false);
        }
        else
        {
            // Show hired state
            if (emptyState != null) emptyState.SetActive(false);
            if (hiredState != null) hiredState.SetActive(true);

            // Update hired state UI
            UpdateHiredStateDisplay(coach);
        }
    }

    private void UpdateHiredStateDisplay(CoachData coach)
    {
        if (nameText != null)
            nameText.text = coach.coachName;

        if (salaryText != null)
            salaryText.text = $"${coach.weeklySalary:N0}/wk";

        if (ratingText != null)
            ratingText.text = "Rating :" + $"{coach.starRating} Stars";

        if (DEFText != null)
            DEFText.text = "DEF +" + $"{coach.defenseBonus}" + ", OFF +" + $"{coach.offenseBonus}";


    }

    private void UpdateCoach() 
    {
        if (CoachManager.instance != null)
        {
            if (type == CoachType.Offense)
            {
                assignedCoach = CoachManager.instance.offenseCoach;
                if (assignedCoach != null)
                {
                    Debug.Log($"[CoachSlotUI] Assigning offense coach: {assignedCoach.coachName}");
                }
            }
            else if (type == CoachType.Defense) 
            {
                assignedCoach = CoachManager.instance.defenseCoach;
                if (assignedCoach != null)
                {
                    Debug.Log($"[CoachSlotUI] Assigning defense coach: {assignedCoach.coachName}");
                }
            }
        }
    }
/*
    private void FireCoach()
    {
        if (currentCoach != null && coachManager != null)
        {
            coachManager.FireCoach(slotType);
        }
    }*/

}
