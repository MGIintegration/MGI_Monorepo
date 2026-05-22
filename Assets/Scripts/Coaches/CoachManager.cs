using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

public class CoachManager : MonoBehaviour
{
    public static CoachManager instance;

    public Button fireOffence;
    public Button fireDefense;

    [Header("Budget Display")]
    public TextMeshProUGUI mainScreenBudgetText;

    private IDisposable hireSubscription;
    private IDisposable fireSubscription;

    public static event Action<CoachData, CoachType> OnCoachHired;
    public static event Action<CoachType> OnCoachFired;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnEnable()
    {
        hireSubscription = EventBus.Subscribe("hire_coach", _ => UpdateBudgetDisplay());
        fireSubscription = EventBus.Subscribe("fire_coach", _ => UpdateBudgetDisplay());
    }

    private void OnDisable()
    {
        hireSubscription?.Dispose();
        fireSubscription?.Dispose();
    }

    private void Start()
    {
        if (fireOffence != null)
            fireOffence.onClick.AddListener(() => CoachesService.FireCoach("O"));
        if (fireDefense != null)
            fireDefense.onClick.AddListener(() => CoachesService.FireCoach("D"));

        UpdateBudgetDisplay();
    }

    private void UpdateBudgetDisplay()
    {
        if (mainScreenBudgetText == null) return;
        var wallet = new EconomyService().GetWallet(CoachesService.LocalPlayerId);
        mainScreenBudgetText.text = wallet != null
            ? $"WEEKLY BUDGET: {wallet.coins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} COINS"
            : "WEEKLY BUDGET: --";
    }
}

[System.Serializable]
public class TeamBonus
{
    public int offenseBonus;
    public int defenseBonus;
    public int specialTeamsBonus;
    public int TotalBonus => offenseBonus + defenseBonus + specialTeamsBonus;
}
