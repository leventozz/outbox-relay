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
        private readonly int _maxRetryCount = 10;

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

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var rabbitMqPublisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

            var pendingOutboxes = await outboxRepository.ClaimPendingMessagesAsync();

            if (!pendingOutboxes.Any())
            {
                _logger.LogDebug("No pending outbox record to be processed was found.");
                return;
            }

            var successfulOutboxes = new List<Guid>();
            var failedOutboxes = new List<(Guid Id, int NewRetryCount, string ErrorMessage, bool IsMaxRetryReached)>();

            foreach (var outbox in pendingOutboxes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("The process has been canceled, the outbox processing is being stopped.");
                    break;
                }

                try
                {
                    CreateTransactionMessage? message;

                    try
                    {
                        message = JsonSerializer.Deserialize<CreateTransactionMessage>(outbox.Payload, JsonDefaults.Default);

                        if (message == null)
                        {
                            var errorMsg = "The outbox payload could not be deserialized.";
                            _logger.LogError($"{errorMsg} OutboxId: {outbox.Id}");
                            failedOutboxes.Add((outbox.Id, outbox.RetryCount, errorMsg, true));
                            continue;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx,
                            "The outbox payload contains invalid JSON and cannot be deserialized. OutboxId: {OutboxId}",
                            outbox.Id);

                        failedOutboxes.Add((outbox.Id, outbox.RetryCount, $"Invalid JSON: {jsonEx.Message},", true));
                        continue;
                    }

                    //at least once. check duplicates on consumer side
                    await rabbitMqPublisher.PublishAsync(message, cancellationToken);
                    successfulOutboxes.Add(outbox.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing the outbox record. OutboxId: {OutboxId}, RetryCount: {RetryCount}",
                        outbox.Id, outbox.RetryCount);

                    var newRetryCount = outbox.RetryCount + 1;
                    var errorMessage = $"{ex.GetType().Name}: {ex.Message}";

                    var isMaxRetryReached = newRetryCount >= _maxRetryCount;

                    if (isMaxRetryReached)
                    {
                        _logger.LogWarning(
                            "The outbox record has reached the maximum retry count and will be marked as Failed. OutboxId: {OutboxId}, RetryCount: {RetryCount}",
                            outbox.Id, newRetryCount);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "The outbox record will be retried. OutboxId: {OutboxId}, RetryCount: {RetryCount}/{MaxRetry}",
                            outbox.Id, newRetryCount, _maxRetryCount);
                    }

                    failedOutboxes.Add((outbox.Id, newRetryCount, errorMessage, isMaxRetryReached));
                }
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (successfulOutboxes.Any())
                {
                    await outboxRepository.BulkUpdateStatusAsync(successfulOutboxes, (short)OutboxStatus.Completed);
                }

                if (failedOutboxes.Any())
                {
                    var maxRetryReachedIds = failedOutboxes.Where(f => f.IsMaxRetryReached).Select(f => f.Id).ToList();
                    var retryableIds = failedOutboxes.Where(f => !f.IsMaxRetryReached).Select(f => f.Id).ToList();

                    if (maxRetryReachedIds.Any())
                    {
                        await outboxRepository.BulkUpdateStatusAsync(maxRetryReachedIds, (short)OutboxStatus.Failed);
                    }

                    if (retryableIds.Any())
                    {
                        await outboxRepository.BulkUpdateStatusAsync(retryableIds, (short)OutboxStatus.Pending);
                    }

                    var retryInfos = failedOutboxes.Select(f => (f.Id, f.NewRetryCount, f.ErrorMessage)).ToList();
                    await outboxRepository.BulkUpdateRetryInfoAsync(retryInfos);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update outbox statuses in database. Rolling back transaction.");
                await transaction.RollbackAsync(cancellationToken);
            }
        }
    }
}
