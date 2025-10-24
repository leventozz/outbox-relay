using OutboxRelay.Infrastructure.Models;

namespace OutboxRelay.Infrastructure.Repositories.Outboxes
{
    public interface IOutboxRepository
    {
        Task<Outbox> CreateAsync(Outbox outbox);
        Task<Outbox?> GetByIdAsync(Guid id);
        Task<IEnumerable<Outbox>> GetAndLockPendingAsync(int batchSize = 5);
        Task<Outbox> UpdateStatusAsync(Guid id, short status, string? errorMessage = null);
        Task<Outbox> UpdateRetryInfoAsync(Guid id, int retryCount, string? errorMessage = null);
        Task<int> BulkDeleteCompletedAsync(int olderThanDays);
    }
}
