using OutboxRelay.Application.Abstractions;
using OutboxRelay.Common.Messaging;
using OutboxRelay.Common.Options;
using OutboxRelay.Core.Enums;
using OutboxRelay.Core.Models;
using System.Text.Json;

namespace OutboxRelay.Application.Features.Transactions
{
    public class TransactionApplication : ITransactionApplication
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionApplication(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
            await _unitOfWork.BeginTransactionAsync();
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

                CreateTransactionMessage createTransactionMessage = new CreateTransactionMessage
                {
                    Id = transactionEntity.Id
                };

                var payload = JsonSerializer.Serialize(createTransactionMessage, JsonDefaults.Default);

                var outboxEntity = new Outbox
                {
                    Id = Guid.NewGuid(),
                    Payload = payload,
                    Status = (short)OutboxStatus.Pending,
                    RetryCount = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                
                await _unitOfWork.TransactionRepository.CreateAsync(transactionEntity);
                await _unitOfWork.OutboxRepository.CreateAsync(outboxEntity);

                await _unitOfWork.CommitAsync();

                return transactionEntity;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
