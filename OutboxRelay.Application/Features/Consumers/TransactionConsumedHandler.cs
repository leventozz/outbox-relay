using Microsoft.Extensions.Logging;
using OutboxRelay.Application.Abstractions;
using OutboxRelay.Common.Messaging;
using OutboxRelay.Core.Enums;
using OutboxRelay.Core.Models;

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
            var transaction = await _uow.TransactionRepository.GetByIdAsync(message.Id);

            if (transaction == null)
            {
                _logger.LogError(
                    "The consumed transaction record could not be found in the database. TransactionId: {TransactionId}",
                    message.Id);
                throw new InvalidOperationException($"Transaction {message.Id} not found in database.");
            }

            if (transaction.Status != (short)TransactionStatus.Pending)
            {
                _logger.LogWarning(
                    "The transaction has already been processed. Status: {Status}. Message ACK to be sent. TransactionId: {TransactionId}",
                    (TransactionStatus)transaction.Status,
                    transaction.Id);

                return;
            }

            transaction.Status = (short)TransactionStatus.Completed;

            //getbyid already tracked by ef core
            //await _uow.TransactionRepository.UpdateStatusAsync(transaction.Id, (short)TransactionStatus.Completed); 

            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
