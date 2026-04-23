using TMPro;
using UnityEngine;
using System.Globalization;

public class BudgetHud : MonoBehaviour
{
    [Header("UI refs (assign in Inspector)")]
    public TMP_Text budgetText;
    public TMP_Text recoveryBoostText;

    [Header("IDs")]
    public string teamId;

    private BudgetHudMiddleware budgetHudMiddleware;

    void Awake()
    {
        if (!budgetText) Debug.LogError("[BudgetHud] 'budgetText' is not assigned in Inspector.");
        if (string.IsNullOrWhiteSpace(teamId)) Debug.LogError("[BudgetHud] 'teamId' is empty.");

        Debug.Log("DEVICE_ID = " + DeviceIdProvider.GetOrCreateDeviceId());

        budgetHudMiddleware = new BudgetHudMiddleware();
    }

    void OnEnable() => Refresh();
    void Start() => Refresh();

    public void Refresh()
    {
        var result = budgetHudMiddleware.TryGetBudget(teamId);

        if (!result.Success)
        {
            Debug.LogError("[BudgetHud] " + result.Message);

            if (budgetText)
                budgetText.text = "FACILITY BUDGET: [no data]";

            if (recoveryBoostText)
                recoveryBoostText.text = "RECOVERY BOOST: [no data]";

            return;
        }

        if (budgetText)
        {
            var usd = result.Budget.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
            budgetText.text = $"FACILITY BUDGET: {usd}";
        }

        if (recoveryBoostText)
        {
            recoveryBoostText.text = $"RECOVERY BOOST: +{result.RecoveryBoostPercent:0.#}%";
        }
    }
}