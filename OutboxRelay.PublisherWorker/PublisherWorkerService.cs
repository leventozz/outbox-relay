using OutboxRelay.Application.Abstractions;
using OutboxRelay.Common.Messaging;
using OutboxRelay.Common.Options;
using OutboxRelay.Core.Enums;
using OutboxRelay.Core.Models;
using OutboxRelay.Infrastructure.Publisher.Abstractions;
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

            var pendingOutboxes = await outboxRepository.ClaimPendingMessagesAsync();

            if (!pendingOutboxes.Any())
            {
                _logger.LogDebug("No pending outbox record to be processed was found.");
                return;
            }

            foreach (var outbox in pendingOutboxes)
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
                CreateTransactionMessage? message;

                try
                {
                    message = JsonSerializer.Deserialize<CreateTransactionMessage>(outboxItem.Payload, JsonDefaults.Default);

                    if (message == null)
                    {
                        var errorMsg = "The outbox payload could not be deserialized.";
                        _logger.LogError($"{errorMsg} OutboxId: {outboxItem.Id}");
                        await outboxRepository.UpdateStatusAsync(outboxItem.Id, (short)OutboxStatus.Failed);
                        return;
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx,
                        "The outbox payload contains invalid JSON and cannot be deserialized. OutboxId: {OutboxId}",
                        outboxItem.Id);

                    await outboxRepository.UpdateStatusAsync(
                        outboxItem.Id,
                        (short)OutboxStatus.Failed,
                        $"Invalid JSON: {jsonEx.Message}");

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
                    var errorMsg = "The outbox record has reached the maximum retry count and will be marked as Failed.";
                    _logger.LogWarning($"{errorMsg} OutboxId: {outboxItem.Id}, RetryCount: {outboxItem.RetryCount}",
                        outboxItem.Id, newRetryCount);

                    await outboxRepository.UpdateStatusAsync(outboxItem.Id, (short)OutboxStatus.Failed);
                }
                else
                {
                    _logger.LogInformation(
                        "The outbox record will be retried. OutboxId: {OutboxId}, RetryCount: {RetryCount}/{MaxRetry}",
                        outboxItem.Id, newRetryCount, _maxRetryCount);

                    await outboxRepository.UpdateStatusAsync(outboxItem.Id, (short)OutboxStatus.Pending);
                }

                await outboxRepository.UpdateRetryInfoAsync(outboxItem.Id, newRetryCount, errorMessage);
            }
        }
    }
}
