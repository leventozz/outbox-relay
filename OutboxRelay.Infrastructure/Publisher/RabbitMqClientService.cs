using Microsoft.Extensions.Logging;
using OutboxRelay.Common.Const;
using OutboxRelay.Common.Exceptions;
using RabbitMQ.Client;

namespace OutboxRelay.Infrastructure.Publisher
{
    public class RabbitMqClientService : IAsyncDisposable
    {
        private IConnection? _connection;
        private bool _isSetupTopology = false;
        private readonly ConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMqClientService> _logger;
        private static readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        public RabbitMqClientService(ConnectionFactory connectionFactory, ILogger<RabbitMqClientService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    _connection = await _connectionFactory.CreateConnectionAsync();

                #region Exchange and queue setup

                if (!_isSetupTopology)
                {
                    await using var channel = await _connection.CreateChannelAsync();

                    //direct exchange create
                    await channel.ExchangeDeclareAsync(exchange: RabbitMqConstants.TransactionExchangeName, type: ExchangeType.Direct, durable: true);

                    //queue create
                    await channel.QueueDeclareAsync(RabbitMqConstants.TransactionQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                    //queue bind
                    await channel.QueueBindAsync(queue: RabbitMqConstants.TransactionQueueName, exchange: RabbitMqConstants.TransactionExchangeName, routingKey: RabbitMqConstants.TransactionCreateRoutingKey);

                    //logging
                    _logger.LogInformation(
                        "RabbitMQ topology setup completed - Exchange: {Exchange}, Queue: {Queue}",
                        RabbitMqConstants.TransactionExchangeName,
                        RabbitMqConstants.TransactionQueueName);
                    _isSetupTopology = true;
                }

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(
                        "RabbitMQ topology setup NOT completed - Exchange: {Exchange}, Queue: {Queue}",
                        RabbitMqConstants.TransactionExchangeName,
                        RabbitMqConstants.TransactionQueueName);

                throw new RabbitMqTopologyException(
                    RabbitMqConstants.TransactionExchangeName,
                    RabbitMqConstants.TransactionQueueName,
                    ex);
            }
            finally
            {
                _connectionLock.Release();
            }
            return _connection;
        }

        public async ValueTask DisposeAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                    _logger.LogInformation("RabbitMQ connection disposed");
                }
            }
            finally
            {
                _connectionLock.Release();
                _connectionLock.Dispose();
            }
        }
    }
}
