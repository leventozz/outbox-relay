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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly RabbitMqClientService _rabbitMqClientService;
        private IConnection _connection;
        private IChannel _channel;
        private AsyncEventingBasicConsumer _consumer;

        public ConsumerWorkerService(ILogger<ConsumerWorkerService> logger, IServiceScopeFactory serviceScopeFactory, RabbitMqClientService rabbitMqClientService)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _rabbitMqClientService = rabbitMqClientService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _connection = await _rabbitMqClientService.GetConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

                _consumer = new AsyncEventingBasicConsumer(_channel);

                _consumer.ReceivedAsync += (sender, @event) =>
                    Consumer_ReceivedAsync(sender, @event, cancellationToken); 

                await _channel.BasicConsumeAsync(
                    queue: RabbitMqConstants.TransactionQueueName,
                    autoAck: false,
                    consumer: _consumer);

                _logger.LogInformation("Consumer subscribed to queue. Waiting for messages.");


                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Consumer Worker Service cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Consumer Worker Service failed critically.");
            }
        }

        private async Task Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event, CancellationToken cancellationToken)
        {
            CreateTransactionMessage createTransactionMessage;

            try
            {
                createTransactionMessage = JsonSerializer.Deserialize<CreateTransactionMessage>(
                    Encoding.UTF8.GetString(@event.Body.ToArray()),
                    JsonDefaults.Default);

                if (createTransactionMessage == null)
                {
                    throw new InvalidOperationException("Deserialized message is null.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize message. Sending to NACK.");
                await _channel.BasicNackAsync(@event.DeliveryTag, false, false);
                return;
            }

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var consumerHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<CreateTransactionMessage>>();
                try
                {
                    await consumerHandler.HandleAsync(createTransactionMessage, cancellationToken);
                    await _channel.BasicAckAsync(deliveryTag: @event.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message. Sending to NACK.");

                    await _channel.BasicNackAsync(
                        deliveryTag: @event.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Consumer Worker Service...");
            await _channel?.CloseAsync();
            await _connection?.CloseAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
