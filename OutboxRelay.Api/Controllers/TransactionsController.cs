using Microsoft.AspNetCore.Mvc;
using OutboxRelay.Api.Models;
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
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutboxRepository _outboxRepository;

        public TransactionsController(ITransactionRepository transactionRepository, IOutboxRepository outboxRepository)
        {
            _transactionRepository = transactionRepository;
            _outboxRepository = outboxRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            try
            {
                var transaction = new Transaction
                {
                    FromAccountId = request.FromAccountId,
                    ToAccountId = request.ToAccountId,
                    Amount = request.Amount,
                    Status = (short)TransactionStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createdTransaction = await _transactionRepository.CreateAsync(transaction);

                var outboxEvent = new Outbox
                {
                    Payload = JsonSerializer.Serialize(createdTransaction),
                    Status = (short)OutboxStatus.Pending,
                    RetryCount = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _outboxRepository.CreateAsync(outboxEvent);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the transaction", error = ex.Message });
            }
        }

    }
}
