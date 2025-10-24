using OutboxRelay.Common.Messaging;

namespace OutboxRelay.Infrastructure.Publisher
{
    public interface IRabbitMqPublisher
    {
        Task PublishAsync(CreateTransactionMessage createTransactionMessage, CancellationToken cancellationToken);
    }
}
