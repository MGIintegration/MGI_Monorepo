using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoachSlotUI : MonoBehaviour
{
    public CoachType type;

    [Header("UI States")]
    public GameObject emptyState;
    public GameObject hiredState;

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

    [Header("Navigation")]
    public GameObject coachDetailsScreen;
    public GameObject mainScreen;
    public GameObject hiringMarketScreen;

    private IDisposable hireSubscription;
    private IDisposable fireSubscription;

    private void Start()
    {
        if (fireCoachButton != null)
            fireCoachButton.onClick.AddListener(OnFireButtonClicked);

        if (viewCoachButton != null)
            viewCoachButton.onClick.AddListener(OnViewButtonClicked);

        if (hireCoachButton != null)
            hireCoachButton.onClick.AddListener(OnHireCoachButtonClicked);
    }

    private void OnHireCoachButtonClicked()
    {
        if (hiringMarketScreen == null) return;
        if (mainScreen != null) mainScreen.SetActive(false);
        hiringMarketScreen.SetActive(true);
    }

    private void OnViewButtonClicked()
    {
        if (coachDetailsScreen == null) return;
        var state = CoachesService.GetTeamState();
        var coachId = GetAssignedCoachId(state, GetServiceCoachType());
        if (string.IsNullOrEmpty(coachId)) return;
        var populator = coachDetailsScreen.GetComponentInChildren<CoachProfilePopulator>(true);
        if (populator != null)
            populator.PopulateFromRecord(CoachesService.GetCoachById(coachId));
        if (mainScreen != null) mainScreen.SetActive(false);
        coachDetailsScreen.SetActive(true);
    }

    private void OnEnable()
    {
        hireSubscription = EventBus.Subscribe("hire_coach", OnHireCoachEvent);
        fireSubscription = EventBus.Subscribe("fire_coach", OnFireCoachEvent);
        RefreshFromService();
    }

    private void OnDisable()
    {
        hireSubscription?.Dispose();
        fireSubscription?.Dispose();
    }

    private void OnHireCoachEvent(EventBus.EventEnvelope evt)
    {
        var payload = JsonUtility.FromJson<HireCoachPayload>(evt.payloadJson);
        if (payload != null && string.Equals(payload.coach_type, GetServiceCoachType(), StringComparison.Ordinal))
            RefreshFromService();
    }

    private void OnFireCoachEvent(EventBus.EventEnvelope evt)
    {
        var payload = JsonUtility.FromJson<FireCoachPayload>(evt.payloadJson);
        if (payload != null && string.Equals(payload.coach_type, GetServiceCoachType(), StringComparison.Ordinal))
            RefreshFromService();
    }

    private void OnFireButtonClicked()
    {
        CoachesService.FireCoach(GetServiceCoachType());
    }

    private void RefreshFromService()
    {
        var serviceType = GetServiceCoachType();
        if (string.IsNullOrEmpty(serviceType)) return;

        var state = CoachesService.GetTeamState();
        var coachId = GetAssignedCoachId(state, serviceType);

        if (string.IsNullOrEmpty(coachId))
        {
            UpdateDisplay(null);
            return;
        }

        UpdateDisplay(CoachesService.GetCoachById(coachId));
    }

    private static string GetAssignedCoachId(TeamState state, string coachType)
    {
        if (state == null) return null;
        switch (coachType)
        {
            case "O": return state.offence_coach;
            case "D": return state.defence_coach;
            case "S": return state.special_teams_coach;
            default:  return null;
        }
    }

    private string GetServiceCoachType()
    {
        switch (type)
        {
            case CoachType.Offense:      return "O";
            case CoachType.Defense:      return "D";
            case CoachType.SpecialTeams: return "S";
            default:                     return null;
        }
    }

    public void UpdateDisplay(CoachDatabaseRecord coach)
    {
        if (coach == null)
        {
            if (emptyState != null) emptyState.SetActive(true);
            if (hiredState != null) hiredState.SetActive(false);
            if (nameText != null)   nameText.text   = "Name: —";
            if (salaryText != null) salaryText.text = "Salary: —";
            if (ratingText != null) ratingText.text = "Rating: —";
            if (DEFText != null)    DEFText.text    = "";
            if (OFFText != null)    OFFText.text    = "";
            if (fireCoachButton != null) fireCoachButton.interactable = false;
        }
        else
        {
            if (emptyState != null) emptyState.SetActive(false);
            if (hiredState != null) hiredState.SetActive(true);
            UpdateHiredStateDisplay(coach);
            if (fireCoachButton != null) fireCoachButton.interactable = true;
        }
    }

    private void UpdateHiredStateDisplay(CoachDatabaseRecord coach)
    {
        if (nameText != null)
            nameText.text = coach.coach_name;

        if (salaryText != null)
        {
            float weeklySalary = coach.salary * 1_000_000f / 52f;
            salaryText.text = $"${weeklySalary.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)}/wk";
        }

        if (ratingText != null)
            ratingText.text = $"Rating: {Mathf.RoundToInt(Mathf.Clamp(coach.overall_rating, 1f, 5f))} Stars";

        if (DEFText != null)
        {
            if (GetServiceCoachType() == "S")
            {
                int st = Mathf.RoundToInt((coach.field_goal_accuracy + coach.kickoff_instance + coach.return_speed + coach.return_coverage) / 4f * 5f);
                DEFText.text = $"ST +{st}";
            }
            else
            {
                int def = Mathf.RoundToInt((coach.run_defence + coach.pressure_control + coach.coverage_discipline + coach.turnover) / 4f * 5f);
                int off = Mathf.RoundToInt((coach.passing_efficiency + coach.rush + coach.red_zone_conversion + coach.play_variation) / 4f * 5f);
                DEFText.text = $"DEF +{def}, OFF +{off}";
            }
        }
    }
}
