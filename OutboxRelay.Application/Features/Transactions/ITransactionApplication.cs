using OutboxRelay.Core.Models;

namespace OutboxRelay.Application.Features.Transactions
{
    public interface ITransactionApplication
    {
        Task<Transaction> RegisterTransactionAsync(int fromAccountId, int toAccountId, decimal amount);
    }
}
