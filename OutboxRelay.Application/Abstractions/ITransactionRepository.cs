using OutboxRelay.Core.Models;

namespace OutboxRelay.Application.Abstractions
{
    public interface ITransactionRepository
    {
        Task<Transaction> CreateAsync(Transaction transaction);
        Task<Transaction?> GetByIdAsync(Guid id);
        Task<Transaction> UpdateStatusAsync(Guid id, short status);
    }
}
