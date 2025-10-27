using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OutboxRelay.Application.Abstractions;
using OutboxRelay.Common.Messaging;
using OutboxRelay.Core.Enums;

namespace OutboxRelay.Application.Features.Consumers
{
    public class TransactionConsumedHandler : IMessageHandler<CreateTransactionMessage>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<TransactionConsumedHandler> _logger;

        public TransactionConsumedHandler(IUnitOfWork uow, ILogger<TransactionConsumedHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task HandleAsync(CreateTransactionMessage message, CancellationToken cancellationToken)
        {
            await _uow.BeginTransactionAsync(cancellationToken);

            try
            {
                var transaction = await _uow.TransactionRepository.GetByIdAsync(message.Id);

                if (transaction == null)
                {
                    _logger.LogError(
                        "The consumed transaction record could not be found in the database. TransactionId: {TransactionId}",
                        message.Id);

                    await _uow.CommitAsync(cancellationToken);
                    return;
                }

                if (transaction.Status != (short)TransactionStatus.Pending)
                {
                    _logger.LogWarning(
                        "The transaction has already been processed. Status: {Status}. Message ACK to be sent. TransactionId: {TransactionId}",
                        (TransactionStatus)transaction.Status,
                        transaction.Id);

                    await _uow.CommitAsync(cancellationToken);
                    return;
                }

                // simulate processing the transaction (e.g., updating account balances)

                transaction.Status = (short)TransactionStatus.Completed;

                await _uow.CommitAsync(cancellationToken);

            }
            catch (SqlException ex) 
            {
                _logger.LogWarning(ex, "Transient database error. Rolling back and requesting REQUEUE.");
                await _uow.RollbackAsync(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permanent/Unexpected error in handler. Rolling back. Message will be NACKED (NO REQUEUE).");
                await _uow.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Handler permanent failure", ex);
            }
        }
    }
}
