using ForecastApi.Models;
using ForecastApi.Processors;

namespace ForecastApi.Services;

public class ForecastService
{
    public EconomyForecastResponse RunForecast(ForecastRequest request)
    {
        var forecast = ForecastProcessor.GenerateForecast(request, out var alerts);

        return new EconomyForecastResponse
        {
            PlayerId = request.PlayerId,
            CurrentBalance = request.CurrentBalance,
            Weeks = request.Weeks,
            Salary = request.Salary,
            Income = request.Income,
            Expenses = request.Expenses,
            Bonuses = request.Bonuses,
            Forecast = forecast,
            Alerts = alerts
        };
    }
}
