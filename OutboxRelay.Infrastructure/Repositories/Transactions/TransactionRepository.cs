using Microsoft.EntityFrameworkCore;
using OutboxRelay.Application.Abstractions;
using OutboxRelay.Common.Exceptions;
using OutboxRelay.Core.Models;

namespace OutboxRelay.Infrastructure.Repositories.Transactions
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            return transaction;
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions
                .FromSqlInterpolated($@"
                    SELECT * FROM Transactions WITH (UPDLOCK, ROWLOCK)
                    WHERE Id = {id}
                ")
                .AsTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<Transaction> UpdateStatusAsync(Guid id, short status)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                throw new TransactionNotFoundException(id);
            }

            transaction.Status = status;
            return transaction;
        }
    }
}
