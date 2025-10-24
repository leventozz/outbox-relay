using Microsoft.Extensions.Logging;
using OutboxRelay.Common.Const;
using RabbitMQ.Client;

namespace OutboxRelay.Infrastructure.Publisher
{
    public class RabbitMqClientService : IAsyncDisposable
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly ILogger<RabbitMqClientService> _logger;
        private bool _isSetupDone = false;
        private static readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        public RabbitMqClientService(ConnectionFactory connectionFactory, ILogger<RabbitMqClientService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IChannel> ConnectAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    _connection = await _connectionFactory.CreateConnectionAsync();

                if (_channel == null || !_channel.IsOpen)
                    _channel = await _connection.CreateChannelAsync();

                #region Exchange and queue setup

                if (!_isSetupDone)
                {
                    //direct exchange create
                    await _channel.ExchangeDeclareAsync(exchange: RabbitMqConstants.TransactionExchangeName, type: ExchangeType.Direct, durable: true);

                    //queue create
                    await _channel.QueueDeclareAsync(RabbitMqConstants.TransactionQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                    //queue bind
                    await _channel.QueueBindAsync(queue: RabbitMqConstants.TransactionQueueName, exchange: RabbitMqConstants.TransactionExchangeName, routingKey: RabbitMqConstants.TransactionCreateRouteName);

                    //logging
                    _logger.LogInformation("RabbitMQ connection started");
                    _isSetupDone = true;
                }

                #endregion
            }
            finally
            {
                _connectionLock.Release();
            }
            return _channel;
        }



        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }
            _logger.LogInformation("RabbitMQ connection disposed");
        }
    }
}
