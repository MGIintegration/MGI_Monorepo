using Microsoft.AspNetCore.Mvc;
using WalletApi.Models;
using WalletApi.Services;

namespace WalletApi.Controllers;

[ApiController]
[Route("wallet")]
public sealed class WalletController : ControllerBase
{
    private readonly IWalletService _wallet;

    public WalletController(IWalletService wallet)
    {
        _wallet = wallet;
    }

    // GET /wallet/display?player_id=1
    [HttpGet("display")]
    public ActionResult<WalletDisplayResponse> Display([FromQuery] string player_id)
    {
        if (string.IsNullOrWhiteSpace(player_id))
            return BadRequest(new { detail = "player_id is required" });

        if (!_wallet.TryDisplay(player_id, out var response))
            return NotFound(new { detail = "Player not found" });

        return Ok(response);
    }

    // POST /wallet/update
    [HttpPost("update")]
    public ActionResult<WalletActionResponse> Update([FromBody] WalletUpdateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = _wallet.Update(request);

        if (result.status == "error")
            return BadRequest(result);

        return Ok(result);
    }
}