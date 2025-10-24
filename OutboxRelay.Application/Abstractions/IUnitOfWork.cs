namespace OutboxRelay.Application.Abstractions
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        ITransactionRepository TransactionRepository { get; }
        IOutboxRepository OutboxRepository { get; }
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
