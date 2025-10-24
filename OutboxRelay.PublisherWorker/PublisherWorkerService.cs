using OutboxRelay.Common.Enums;
using OutboxRelay.Common.Messaging;
using OutboxRelay.Common.Options;
using OutboxRelay.Infrastructure.Models;
using OutboxRelay.Infrastructure.Publisher;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using System.Text.Json;

namespace OutboxRelay.PublisherWorkerService
{
    public class PublisherWorkerService : BackgroundService
    {
        private readonly ILogger<PublisherWorkerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);
        private readonly int _maxRetryCount = 3;

        public PublisherWorkerService(ILogger<PublisherWorkerService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while processing outbox messages.");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task ProcessPendingOutboxMessagesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var rabbitMqPublisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

            var pendingOutboxes = await outboxRepository.GetAndLockPendingAsync();

            if (!pendingOutboxes.Any())
            {
                _logger.LogDebug("No pending outbox record to be processed was found.");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var readyToProcess = pendingOutboxes
                .Where(o => IsReadyForRetry(o, now))
                .ToList();

            if (!readyToProcess.Any())
            {
                _logger.LogDebug("No outbox records are ready for retry yet.");
                return;
            }

            foreach (var outbox in readyToProcess)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("The process has been canceled, the outbox processing is being stopped.");
                    break;
                }

                await ProcessSingleOutboxAsync(outbox, outboxRepository, rabbitMqPublisher, cancellationToken);
            }
        }

        private async Task ProcessSingleOutboxAsync(Outbox outboxItem, IOutboxRepository outboxRepository, IRabbitMqPublisher rabbitMqPublisher, CancellationToken cancellationToken)
        {
            try
            {
                var message = JsonSerializer.Deserialize<CreateTransactionMessage>(outboxItem.Payload, JsonDefaults.Default);

                if (message == null)
                {
                    var errorMsg = "The outbox payload could not be deserialized.";
                    _logger.LogError($"{errorMsg} OutboxId: {outboxItem.Id}");
                    await outboxRepository.UpdateStatusAsync(outboxItem.Id, (short)OutboxStatus.Failed);
                    return;
                }

                await rabbitMqPublisher.PublishAsync(message, cancellationToken);

                await outboxRepository.UpdateStatusAsync(outboxItem.Id, (short)OutboxStatus.Completed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the outbox record. OutboxId: {OutboxId}, RetryCount: {RetryCount}",
                    outboxItem.Id, outboxItem.RetryCount);

                var newRetryCount = outboxItem.RetryCount + 1;
                var errorMessage = $"{ex.GetType().Name}: {ex.Message}";

                if (newRetryCount >= _maxRetryCount)
                {
                    _logger.LogWarning("The outbox record has reached the maximum retry count and is marked as Failed. OutboxId: {OutboxId}, RetryCount: {RetryCount}",
                        outboxItem.Id, newRetryCount);

                    await outboxRepository.UpdateStatusAsync(outboxItem.Id, (short)OutboxStatus.Failed);
                }

                await outboxRepository.UpdateRetryInfoAsync(outboxItem.Id, newRetryCount, errorMessage);
            }
        }

        private bool IsReadyForRetry(Outbox outbox, DateTimeOffset now)
        {
            if (outbox.RetryCount == 0 || outbox.LastAttemptAt == null)
                return true;

            var delaySeconds = Math.Pow(2, outbox.RetryCount);
            var nextRetryTime = outbox.LastAttemptAt.Value.AddSeconds(delaySeconds);

            return now >= nextRetryTime;
        }
    }
}
