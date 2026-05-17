using Microsoft.AspNetCore.Mvc;
using TransactionApi.Models;
using TransactionApi.Services;

namespace TransactionApi.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class TransactionController : ControllerBase
{
    private readonly TransactionService _transactionService;

    public TransactionController(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Get transaction history (GET).
    /// </summary>
    [HttpGet("transactions")]
    public ActionResult<List<TransactionResponse>> GetTransactions([FromQuery] string player_id)
    {
        return Ok(_transactionService.GetHistory(player_id));
    }
}
