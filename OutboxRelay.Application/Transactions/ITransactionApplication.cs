using OutboxRelay.Infrastructure.Models;

namespace OutboxRelay.Application.Transactions
{
    public interface ITransactionApplication
    {
        Task<Transaction> CommitAsync(int fromAccountId, int toAccountId, decimal amount);
    }
}
