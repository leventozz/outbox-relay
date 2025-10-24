using Microsoft.AspNetCore.Mvc;
using OutboxRelay.Api.Models;
using OutboxRelay.Application.Transactions;
using OutboxRelay.Common.Enums;
using OutboxRelay.Infrastructure.Models;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using OutboxRelay.Infrastructure.Repositories.Transactions;
using System.Text.Json;

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

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            try
            {
                await _transactionApplication.CommitAsync(request.FromAccountId, request.ToAccountId, request.Amount);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the transaction", error = ex.Message });
            }
        }

    }
}
