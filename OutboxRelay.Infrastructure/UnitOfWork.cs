using Microsoft.EntityFrameworkCore.Storage;
using OutboxRelay.Application.Abstractions;
using OutboxRelay.Core.Models;

namespace OutboxRelay.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;
        public ITransactionRepository TransactionRepository { get; }
        public IOutboxRepository OutboxRepository { get; }

        public UnitOfWork(AppDbContext context, ITransactionRepository transactionRepository, IOutboxRepository outboxRepository)
        {
            _context = context;
            TransactionRepository = transactionRepository;
            OutboxRepository = outboxRepository;
        }

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            _transaction ??= await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            try
            {
                await _context.SaveChangesAsync(ct);

                if (_transaction != null)
                {
                    await _transaction.CommitAsync(ct);
                }
            }
            catch
            {
                await RollbackAsync(ct);
                throw;
            }
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return _context.SaveChangesAsync(ct);
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
        }
    }
}
