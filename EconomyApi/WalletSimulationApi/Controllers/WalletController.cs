using Microsoft.AspNetCore.Mvc;
using WalletApi.Models;
using WalletApi.Services;

namespace WalletApi.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class WalletController : ControllerBase
{
    private readonly WalletService _walletService;

    public WalletController(WalletService walletService)
    {
        _walletService = walletService;
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

