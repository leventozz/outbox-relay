using OutboxRelay.Common.Const;
using OutboxRelay.Common.Messaging;
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

        public async Task Publish(CreateTransactionMessage createTransactionMessage)
        {
            var channel = await _rabbitMqClientService.ConnectAsync();

            var bodyString = JsonSerializer.Serialize(createTransactionMessage);

            var bodyByte = Encoding.UTF8.GetBytes(bodyString);

            BasicProperties properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: RabbitMqConstants.TransactionExchangeName,
                routingKey: RabbitMqConstants.TransactionCreateRouteName,
                mandatory: false,
                basicProperties: properties,
                body: bodyByte);
        }
    }
}
