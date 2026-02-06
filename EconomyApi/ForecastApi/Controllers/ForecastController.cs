using Microsoft.AspNetCore.Mvc;
using ForecastApi.Models;
using ForecastApi.Services;

namespace ForecastApi.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ForecastController : ControllerBase
{
    private readonly ForecastService _forecastService;

    public ForecastController(ForecastService forecastService)
    {
        _forecastService = forecastService;
    }

    /// <summary>
    /// Calculate economy forecast based on income and expenses (POST).
    /// </summary>
    [HttpPost("forecast")]
    public ActionResult<EconomyForecastResponse> ForecastWallet([FromBody] ForecastRequest data)
    {
        return Ok(_forecastService.RunForecast(data));
    }
}
