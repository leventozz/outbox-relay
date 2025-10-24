using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OutboxRelay.Common.Enums;
using OutboxRelay.Common.Exceptions;
using OutboxRelay.Infrastructure.Models;

namespace OutboxRelay.Infrastructure.Repositories.Outboxes
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly AppDbContext _context;

        public OutboxRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Outbox> CreateAsync(Outbox outbox)
        {
            _context.Outboxes.Add(outbox);
            return outbox;
        }

        public async Task<Outbox?> GetByIdAsync(Guid id)
        {
            return await _context.Outboxes
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Outbox>> ClaimPendingMessagesAsync(int batchSize = 5)
        {
            var sql = $@"
                UPDATE TOP (@batchSize) Outboxes
                SET 
                    Status = @newStatus
                OUTPUT 
                    inserted.*
                FROM 
                    Outboxes WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE 
                    Status = @oldStatus;
                ";

            return await _context.Outboxes
                .FromSqlRaw(sql,
                    new SqlParameter("@batchSize", batchSize),
                    new SqlParameter("@newStatus", (short)OutboxStatus.Processing), 
                    new SqlParameter("@oldStatus", (short)OutboxStatus.Pending))
                .ToListAsync();
        }

        public async Task<Outbox> UpdateStatusAsync(Guid id, short status, string? errorMessage = null)
        {
            var outbox = await _context.Outboxes
                .FirstOrDefaultAsync(o => o.Id == id);

            if (outbox == null)
            {
                throw new OutboxNotFoundException(id);
            }

            outbox.Status = status;
            outbox.LastAttemptAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return outbox;
        }

        public async Task<Outbox> UpdateRetryInfoAsync(Guid id, int retryCount, string? errorMessage = null)
        {
            var outbox = await _context.Outboxes
                .FirstOrDefaultAsync(o => o.Id == id);

            if (outbox == null)
            {
                throw new OutboxNotFoundException(id);
            }

            outbox.RetryCount = retryCount;
            outbox.ErrorMessage = errorMessage;
            outbox.LastAttemptAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return outbox;
        }

        public async Task<int> BulkDeleteCompletedAsync(int olderThanDays)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-olderThanDays);

            var outboxesToDelete = await _context.Outboxes
                .Where(o => o.Status == (short)TransactionStatus.Completed &&
                           o.CreatedAt < cutoffDate)
                .ToListAsync();

            if (outboxesToDelete.Any())
            {
                _context.Outboxes.RemoveRange(outboxesToDelete);
                await _context.SaveChangesAsync();
            }

            return outboxesToDelete.Count;
        }
    }
}
