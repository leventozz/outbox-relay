using OutboxRelay.Common.Enums;
using OutboxRelay.Infrastructure.Models;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using OutboxRelay.Infrastructure.Repositories.Transactions;
using System.Text.Json;

namespace OutboxRelay.Application.Transactions
{
    public class TransactionApplication : ITransactionApplication
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly AppDbContext _context;

        public TransactionApplication(
            ITransactionRepository transactionRepository,
            IOutboxRepository outboxRepository,
            AppDbContext context)
        {
            _transactionRepository = transactionRepository;
            _outboxRepository = outboxRepository;
            _context = context;
        }
        /// <summary>
        /// Registers a new transaction between two accounts asynchronously and persists it to the database with atomicity.
        /// </summary>
        /// <remarks>The transaction is initially created with a status of Pending. If the operation
        /// fails, the transaction is rolled back and the exception is propagated to the caller.</remarks>
        /// <param name="fromAccountId">The unique identifier of the account from which the funds will be debited.</param>
        /// <param name="toAccountId">The unique identifier of the account to which the funds will be credited.</param>
        /// <param name="amount">The amount of money to transfer from the source account to the destination account. Must be a positive
        /// value.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the registered transaction
        /// entity.</returns>
        public async Task<Transaction> RegisterTransactionAsync(int fromAccountId, int toAccountId, decimal amount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var transactionEntity = new Transaction
                {
                    Id = Guid.NewGuid(),
                    FromAccountId = fromAccountId,
                    ToAccountId = toAccountId,
                    Amount = amount,
                    Status = (short)TransactionStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var payload = JsonSerializer.Serialize(transactionEntity);

                var outboxEntity = new Outbox
                {
                    Id = Guid.NewGuid(),
                    Payload = payload,
                    Status = (short)OutboxStatus.Pending,
                    RetryCount = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _transactionRepository.CreateAsync(transactionEntity);
                await _outboxRepository.CreateAsync(outboxEntity);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return transactionEntity;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
