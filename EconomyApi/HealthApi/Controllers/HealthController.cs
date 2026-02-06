using Microsoft.AspNetCore.Mvc;
using HealthApi.Models;
using HealthApi.Services;

namespace HealthApi.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly HealthMonitoringService _healthService;

    public HealthController(HealthMonitoringService healthService)
    {
        _healthService = healthService;
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
}

