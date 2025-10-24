using Microsoft.Extensions.Logging;
using OutboxRelay.Common.Const;
using OutboxRelay.Common.Exceptions;
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
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(RabbitMqClientService rabbitMqClientService, ILogger<RabbitMqPublisher> logger)
        {
            _rabbitMqClientService = rabbitMqClientService;
            _logger = logger;
        }

        public async Task PublishAsync(CreateTransactionMessage createTransactionMessage, CancellationToken cancellationToken)
        {
            try
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
                    routingKey: RabbitMqConstants.TransactionCreateRoutingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An error occurred while publishing a message to RabbitMQ. Exchange: {Exchange}, RoutingKey: {RoutingKey}",
                    RabbitMqConstants.TransactionExchangeName,
                    RabbitMqConstants.TransactionCreateRoutingKey);

                throw new RabbitMqPublishException(
                    RabbitMqConstants.TransactionExchangeName,
                    RabbitMqConstants.TransactionCreateRoutingKey,
                    ex);
            }
            
        }
    }
}
