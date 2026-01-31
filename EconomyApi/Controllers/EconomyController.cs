using EconomyApi.Models;
using EconomyApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EconomyApi.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class EconomyController : ControllerBase
{
    private readonly WalletService _walletService;
    private readonly HealthMonitoringService _healthService;

    public EconomyController(WalletService walletService, HealthMonitoringService healthService)
    {
        _walletService = walletService;
        _healthService = healthService;
    }

    /// <summary>
    /// Simulate one week of wallet transactions (POST).
    /// </summary>
    [HttpPost("wallet/simulate")]
    public ActionResult<WalletSimulationResponse> SimulateWallet([FromBody] WalletSimulationRequest data)
    {
        return Ok(_walletService.RunWalletSimulation(data));
    }

    /// <summary>
    /// Get economy health status and failure predictions (GET).
    /// </summary>
    [HttpGet("wallet/health")]
    public ActionResult<HealthMonitoringResponse> GetEconomyHealth(
        [FromQuery] string player_id,
        [FromQuery] int analysis_period_weeks = 4,
        [FromQuery] bool include_predictions = true,
        [FromQuery] bool include_suggestions = true)
    {
        if (!Guid.TryParse(player_id, out var playerId))
            return BadRequest(new { error = "Invalid player_id format." });

        var request = new HealthMonitoringRequest
        {
            PlayerId = playerId,
            AnalysisPeriodWeeks = analysis_period_weeks,
            IncludePredictions = include_predictions,
            IncludeSuggestions = include_suggestions
        };
        return Ok(_healthService.AnalyzePlayerHealth(request));
    }

    /// <summary>
    /// Get health analysis history for a player.
    /// </summary>
    [HttpGet("wallet/health/history")]
    public ActionResult GetHealthHistory([FromQuery] string player_id, [FromQuery] int limit = 10)
    {
        var history = _healthService.GetPlayerHealthHistory(player_id, limit);
        return Ok(history);
    }

    /// <summary>
    /// Get a summary of the player's current health status.
    /// </summary>
    [HttpGet("wallet/health/summary")]
    public ActionResult GetHealthSummary([FromQuery] string player_id)
    {
        var summary = _healthService.GetHealthSummary(player_id);
        return Ok(summary);
    }

    /// <summary>
    /// Browser-friendly GET wallet simulation (query params instead of JSON body).
    /// </summary>
    [HttpGet("wallet/simulate/browser")]
    public ActionResult<WalletSimulationResponse> SimulateWalletBrowser(
        [FromQuery] string? player_id = null,
        [FromQuery] int initial_coins = 1000,
        [FromQuery] int initial_gems = 50,
        [FromQuery] int initial_credits = 25,
        [FromQuery] int daily_income_coins = 100,
        [FromQuery] int daily_income_gems = 2,
        [FromQuery] int daily_expenses_coins = 50,
        [FromQuery] int weekly_bonus_coins = 200,
        [FromQuery] int weekly_bonus_gems = 10)
    {
        var pid = Guid.TryParse(player_id ?? "", out var g) ? g : new Guid("123e4567-e89b-12d3-a456-426614174000");
        var request = new WalletSimulationRequest
        {
            PlayerId = pid,
            CurrentWallet = new WalletState
            {
                Coins = initial_coins,
                Gems = initial_gems,
                Credits = initial_credits
            },
            SimulationParams = new SimulationParameters
            {
                DailyIncomeCoins = daily_income_coins,
                DailyIncomeGems = daily_income_gems,
                DailyExpensesCoins = daily_expenses_coins,
                WeeklyBonusCoins = weekly_bonus_coins,
                WeeklyBonusGems = weekly_bonus_gems,
                SpecialEvents = new Dictionary<string, int>()
            },
            IncludeTransactions = true
        };
        return Ok(_walletService.RunWalletSimulation(request));
    }
}
