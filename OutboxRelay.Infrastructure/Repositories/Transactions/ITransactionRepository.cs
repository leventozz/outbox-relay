using OutboxRelay.Infrastructure.Models;

namespace OutboxRelay.Infrastructure.Repositories.Transactions
{
    public interface ITransactionRepository
    {
        Task<Transaction> CreateAsync(Transaction transaction);
        Task<Transaction?> GetByIdAsync(Guid id);
        Task<Transaction> UpdateStatusAsync(Guid id, short status);
    }
}
