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
    private bool hasStarted;
    private System.IDisposable walletUpdatedSubscription;

    void Awake()
    {
        if (!budgetText) Debug.LogError("[BudgetHud] 'budgetText' is not assigned in Inspector.");
        if (string.IsNullOrWhiteSpace(teamId)) Debug.LogError("[BudgetHud] 'teamId' is empty.");

        budgetHudMiddleware = new BudgetHudMiddleware();
    }

    void OnEnable()
    {
        walletUpdatedSubscription = EventBus.Subscribe("wallet_updated", OnWalletUpdated);

        if (hasStarted)
            Refresh();
    }

    void OnDisable()
    {
        walletUpdatedSubscription?.Dispose();
        walletUpdatedSubscription = null;
    }

    void Start()
    {
        hasStarted = true;
        Refresh();
    }

    private void OnWalletUpdated(EventBus.EventEnvelope evt)
    {
        Refresh();
    }

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
