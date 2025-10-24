using OutboxRelay.Common.Const;
using OutboxRelay.Common.Messaging;
using OutboxRelay.Common.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OutboxRelay.Infrastructure.Publisher
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly RabbitMqClientService _rabbitMqClientService;

        public RabbitMqPublisher(RabbitMqClientService rabbitMqClientService)
        {
            _rabbitMqClientService = rabbitMqClientService;
        }

        public async Task PublishAsync(CreateTransactionMessage createTransactionMessage, CancellationToken cancellationToken)
        {
            var connection = await _rabbitMqClientService.GetConnectionAsync();

            await using var channel = await connection.CreateChannelAsync();

            var payload = JsonSerializer.Serialize(createTransactionMessage, JsonDefaults.Default);

            var body = Encoding.UTF8.GetBytes(payload);

            BasicProperties properties = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: RabbitMqConstants.TransactionExchangeName,
                routingKey: RabbitMqConstants.TransactionCreateRouteName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        }
    }
}
