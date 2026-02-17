using Microsoft.AspNetCore.Mvc;
using SalaryEngineApi.Models;
using SalaryEngineApi.Services;

namespace SalaryEngineApi.Controllers;

[ApiController]
[Route("salary")]
public sealed class SalaryEngineController : ControllerBase
{
    private readonly ISalaryEngineService _engine;

    public SalaryEngineController(ISalaryEngineService engine)
    {
        _engine = engine;
    }

    // -------- 1) Register contract (NO balance) --------
    [HttpPost("contract/register")]
    public ActionResult<SalaryRegisterResponse> RegisterContract([FromBody] SalaryContractRequest request)
    {
        var ok = _engine.RegisterContract(new SalaryContract(
            PlayerId: request.PlayerId,
            BaseSalary: request.BaseSalary,
            BonusMultiplier: request.BonusMultiplier,
            PerformanceThreshold: request.PerformanceThreshold,
            MaxBonusPercentage: request.MaxBonusPercentage
        ));

        if (!ok)
            return BadRequest(new { detail = "salary contract registration failed" });

        return Ok(new SalaryRegisterResponse(
            PlayerId: request.PlayerId,
            TransactionId: Guid.NewGuid().ToString(),
            Currency: "coins",
            Status: "success",
            Message: "Salary contract registered successfully"
        ));
    }

    // -------- 2) Contract details (already NO balance) --------
    [HttpGet("contract/details")]
    public ActionResult<ContractDetailsResponse> GetContractDetails([FromQuery(Name = "player_id")] string playerId)
    {
        try
        {
            var c = _engine.GetContractOrThrow(playerId);
            return Ok(new ContractDetailsResponse(
                PlayerId: c.PlayerId,
                BaseSalary: c.BaseSalary,
                BonusMultiplier: c.BonusMultiplier,
                PerformanceThreshold: c.PerformanceThreshold,
                MaxBonusPercentage: c.MaxBonusPercentage
            ));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { detail = "salary contract not found" });
        }
    }

    // -------- 3) Calculate weekly (NO balance, YES breakdown) --------
    [HttpPost("calculate/weekly")]
    public ActionResult<SalaryWeeklyCostResponse> CalculateWeekly(
        [FromQuery(Name = "player_id")] string playerId,
        [FromBody] PerformanceMetricsRequest metricsRequest
    )
    {
        try
        {
            var metrics = ToMetrics(metricsRequest);
            var (breakdown, _) = _engine.CalculateWeeklyCost(playerId, metrics);

            return Ok(new SalaryWeeklyCostResponse(
                PlayerId: playerId,
                TransactionId: Guid.NewGuid().ToString(),
                Currency: "coins",
                Status: "success",
                Message: "Weekly salary cost calculated",
                SalaryBreakdown: breakdown
            ));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { detail = "salary contract not found" });
        }
    }

    // -------- 4) Trigger weekly (YES balance) --------
    [HttpPost("trigger/weekly")]
    public async Task<ActionResult<SalaryActionResponse>> TriggerWeekly(
        [FromQuery(Name = "player_id")] string playerId,
        [FromBody] PerformanceMetricsRequest metricsRequest
    )
    {
        try
        {
            var metrics = ToMetrics(metricsRequest);

            var (ok, message, newBal, breakdown) =
                await _engine.TriggerWeeklyDeduction(playerId, metrics);

            return Ok(new SalaryActionResponse(
                PlayerId: playerId,
                TransactionId: Guid.NewGuid().ToString(),
                Currency: "coins",
                NewBalance: newBal,
                Status: ok ? "success" : "error",
                Message: message,
                SalaryBreakdown: breakdown
            ));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { detail = "salary contract not found" });
        }
    }

    // -------- 5) Trigger bulk (no per-player balance) --------
    [HttpPost("trigger/bulk")]
    public async Task<ActionResult<BulkSalaryDeductionResponse>> TriggerBulk(
        [FromBody] List<BulkSalaryDeductionItem> items
    )
    {
        var mapped = items.Select(i => (i.PlayerId, ToMetrics(new PerformanceMetricsRequest(
            LeadsGenerated: i.LeadsGenerated,
            ConversionRate: i.ConversionRate,
            QualityScore: i.QualityScore,
            TeamPerformance: i.TeamPerformance
        ))));

        var resp = await _engine.TriggerBulk(mapped);
        return Ok(resp);
    }

    private static PerformanceMetrics ToMetrics(PerformanceMetricsRequest r)
        => new PerformanceMetrics(
            LeadsGenerated: r.LeadsGenerated,
            ConversionRate: r.ConversionRate,
            QualityScore: r.QualityScore,
            TeamPerformance: r.TeamPerformance
        );
}