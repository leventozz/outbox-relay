using OutboxRelay.Common.Messaging;

namespace OutboxRelay.Infrastructure.Publisher.Abstractions
{
    public interface IRabbitMqPublisher
    {
        Task PublishAsync(CreateTransactionMessage createTransactionMessage, CancellationToken cancellationToken);
    }
}
