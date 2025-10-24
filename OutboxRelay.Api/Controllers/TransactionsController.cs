using Microsoft.AspNetCore.Mvc;
using OutboxRelay.Api.Models;
using OutboxRelay.Application.Transactions;

namespace OutboxRelay.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionApplication _transactionApplication;

        public TransactionsController(ITransactionApplication transactionApplication)
        {
            _transactionApplication = transactionApplication;
        }

        [HttpPost("CreateTransaction")]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            var pendingTransaction = await _transactionApplication.RegisterTransactionAsync(request.FromAccountId, request.ToAccountId, request.Amount);
            return Ok(pendingTransaction);
        }

    }
}
