using TMPro;
using UnityEngine;

public class WeeklyForecastUI : MonoBehaviour
{
    [Header("Text Reference")]
    [SerializeField] private TMP_Text forecastText;

    /// <summary>
    /// Updates the weekly forecast value text.
    /// </summary>
    /// <param name="forecastChange">The net change in coins for the week (e.g. -200 or +150).</param>
    public void SetForecast(int forecastChange)
    {
        if (forecastText == null)
        {
            Debug.LogWarning("Forecast Text not assigned in WeeklyForecastUI.");
            return;
        }

        // Format forecast with a sign and unit
        string formatted = $"{forecastChange:+#;-#;0} Coins";

        forecastText.text = formatted;
    }
}
