using OutboxRelay.Application.Features.Consumers;
using OutboxRelay.Common.Const;
using OutboxRelay.Common.Messaging;
using OutboxRelay.Common.Options;
using OutboxRelay.Infrastructure.Publisher;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OutboxRelay.ConsumerWorkerService
{
    public class ConsumerWorkerService : BackgroundService
    {
        private readonly ILogger<ConsumerWorkerService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly RabbitMqClientService _rabbitMqClientService;

        public ConsumerWorkerService(ILogger<ConsumerWorkerService> logger, IServiceScopeFactory serviceScopeFactory, RabbitMqClientService rabbitMqClientService)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _rabbitMqClientService = rabbitMqClientService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeAndProcessTransactionMessage(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while processing outbox messages.");
                }

                await Task.Delay(_pollingInterval, cancellationToken);
            }
        }

        private async Task ConsumeAndProcessTransactionMessage(CancellationToken cancellationToken)
        {
            var connection = await _rabbitMqClientService.GetConnectionAsync();

            await using var channel = await connection.CreateChannelAsync();

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            await channel.BasicConsumeAsync(
                queue: RabbitMqConstants.TransactionQueueName,
                autoAck: false,
                consumer: consumer);

            consumer.ReceivedAsync += Consumer_ReceivedAsync;
        }

        private async Task Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event)
        {
            var consumer = (AsyncEventingBasicConsumer)sender;
            var channel = consumer.Channel;

            var createTransactionMessage = JsonSerializer.Deserialize<CreateTransactionMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()), JsonDefaults.Default);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var consumerHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<CreateTransactionMessage>>();
                try
                {
                    await consumerHandler.HandleAsync(createTransactionMessage, CancellationToken.None);
                    await channel.BasicAckAsync(deliveryTag: @event.DeliveryTag, multiple: false);
                }
                catch (Exception)
                {
                    await channel.BasicNackAsync(
                        deliveryTag: @event.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }

            }
        }
    }
}
