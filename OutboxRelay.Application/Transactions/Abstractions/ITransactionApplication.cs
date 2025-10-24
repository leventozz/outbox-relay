using OutboxRelay.Infrastructure.Models;

namespace OutboxRelay.Application.Transactions.Abstractions
{
    public interface ITransactionApplication
    {
        Task<Transaction> RegisterTransactionAsync(int fromAccountId, int toAccountId, decimal amount);
    }
}
