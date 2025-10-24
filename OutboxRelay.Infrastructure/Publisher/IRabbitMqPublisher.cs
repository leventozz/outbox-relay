using OutboxRelay.Common.Messaging;

namespace OutboxRelay.Infrastructure.Publisher
{
    public interface IRabbitMqPublisher
    {
        Task Publish(CreateTransactionMessage createTransactionMessage)
    }
}
