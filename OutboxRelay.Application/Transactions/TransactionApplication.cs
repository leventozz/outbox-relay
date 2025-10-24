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

        public async Task<Transaction> CommitAsync(int fromAccountId, int toAccountId, decimal amount)
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
