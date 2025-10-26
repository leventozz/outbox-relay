using OutboxRelay.Core.Models;

namespace OutboxRelay.Application.Abstractions
{
    public interface IOutboxRepository
    {
        Task<Outbox> CreateAsync(Outbox outbox);
        Task<Outbox?> GetByIdAsync(Guid id);
        Task<IEnumerable<Outbox>> ClaimPendingMessagesAsync(int batchSize = 5);
        Task<Outbox> UpdateStatusAsync(Guid id, short status, string? errorMessage = null);
        Task<Outbox> UpdateRetryInfoAsync(Guid id, int retryCount, string? errorMessage = null);
        Task<int> BulkDeleteCompletedAsync(int olderThanDays);
        Task BulkUpdateStatusAsync(IEnumerable<Guid> ids, short status);
        Task BulkUpdateRetryInfoAsync(IEnumerable<(Guid Id, int RetryCount, string ErrorMessage)> retryInfos);
    }
}
