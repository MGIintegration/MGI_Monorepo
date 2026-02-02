using TMPro;
using UnityEngine;

public class EconomyForecastPanel : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text earningsText;
    [SerializeField] private TMP_Text expensesText;
    [SerializeField] private TMP_Text netDeltaText;

    [Header("Linked UI References")]
    [SerializeField] private WeeklyForecastUI weeklyForecastUI;  // 👈 This is the important one

    private int earnings;
    private int expenses;
    private int netDelta;

    private void OnEnable()
    {
        UpdateEconomyData();
    }

    private void UpdateEconomyData()
    {
        // Example values
        earnings = 1900;
        expenses = 2100;
        netDelta = earnings - expenses;

        // Update the UI Texts
        if (earningsText) earningsText.text = $"Earnings: {earnings:N0}";
        if (expensesText) expensesText.text = $"Expenses: {expenses:N0}";
        if (netDeltaText) netDeltaText.text = $"Net Delta: {netDelta:+#;-#;0}";

        // Update the top bar forecast
        if (weeklyForecastUI != null)
            weeklyForecastUI.SetForecast(netDelta);
    }
}
