using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OutboxRelay.Application.Abstractions;
using OutboxRelay.Common.Exceptions;
using OutboxRelay.Core.Enums;
using OutboxRelay.Core.Models;
using System.Data;

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
            await _context.Outboxes.AddAsync(outbox);
            return outbox;
        }

        public async Task<Outbox?> GetByIdAsync(Guid id)
        {
            return await _context.Outboxes
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        /// <summary>
        /// Claims a batch of pending outbox messages for processing. 
        /// This method updates up to batchsize messages from the Outboxes table 
        /// that are in the Pending status and are eligible for retry based on exponential backoff logic.
        /// It sets their status to Processing, updates the LastAttemptAt timestamp, 
        /// and returns the updated records. Row-level locks are used to prevent concurrent updates 
        /// and skip already locked rows, ensuring safe parallel processing.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Outbox>> ClaimPendingMessagesAsync(int batchSize = 5)
        {
            var sql = $@"
                UPDATE TOP (@batchSize) Outboxes
                SET 
                    Status = @newStatus,
                    LastAttemptAt = GETUTCDATE() 
                OUTPUT 
                    inserted.*
                FROM 
                    Outboxes WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE 
                    Status = @oldStatus 
                    AND (
                        RetryCount = 0 
                        OR 
                        DATEADD(second, POWER(2, RetryCount), LastAttemptAt) <= GETUTCDATE()
                    );
            ";

            SqlParameter[] parameters =
            [
                new() { ParameterName = "@batchSize", Value = batchSize, SqlDbType = SqlDbType.Int },
                new() { ParameterName = "@newStatus", Value = (short)OutboxStatus.Processing, SqlDbType = SqlDbType.SmallInt },
                new() { ParameterName = "@oldStatus", Value = (short)OutboxStatus.Pending, SqlDbType = SqlDbType.SmallInt }
            ];

            return await _context.Outboxes
                .FromSqlRaw(sql, parameters)
                .AsNoTracking()
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

        public async Task BulkUpdateStatusAsync(IEnumerable<Guid> ids, short status)
        {
            var idList = ids.ToList();

            if (!idList.Any()) 
                return;

            await _context.Outboxes
                .Where(o => idList.Contains(o.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Status, status)
                    .SetProperty(o => o.LastAttemptAt, DateTimeOffset.UtcNow));
        }

        public async Task BulkUpdateRetryInfoAsync(IEnumerable<(Guid Id, int RetryCount, string ErrorMessage)> retryInfos)
        {
            var retryList = retryInfos.ToList();

            if (!retryList.Any()) 
                return;

            var ids = retryList.Select(r => r.Id).ToList();
            var outboxes = await _context.Outboxes
                .Where(o => ids.Contains(o.Id))
                .ToListAsync();

            foreach (var outbox in outboxes)
            {
                var retryInfo = retryList.First(r => r.Id == outbox.Id);
                outbox.RetryCount = retryInfo.RetryCount;
                outbox.ErrorMessage = retryInfo.ErrorMessage;
                outbox.LastAttemptAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
