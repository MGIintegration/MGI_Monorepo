using ForecastApi.Models;

namespace ForecastApi.Processors;

public static class ForecastProcessor
{
    public static List<WeeklyForecast> GenerateForecast(ForecastRequest request, out List<string> alerts)
    {
        var forecast = new List<WeeklyForecast>();
        alerts = new List<string>();
        int currentBalance = request.CurrentBalance;

        for (int i = 1; i <= request.Weeks; i++)
        {
            string weekKey = i.ToString();
            
            int weeklyBonus = request.Bonuses.TryGetValue(weekKey, out int bonus) ? bonus : 0;
            int weeklyExpense = request.Expenses.TryGetValue(weekKey, out int expense) ? expense : 0;

            // Calculation based on JSON sample: Salary + Bonus - Expense
            int weeklyNetChange = request.Salary + weeklyBonus - weeklyExpense;
            currentBalance += weeklyNetChange;

            forecast.Add(new WeeklyForecast
            {
                Week = i,
                NetChange = weeklyNetChange,
                Balance = currentBalance
            });

            if (currentBalance < 0)
            {
                alerts.Add($"Projected negative balance in Week {i}");
            }
        }

        return forecast;
    }
}
