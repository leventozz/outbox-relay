using Microsoft.EntityFrameworkCore;
using OutboxRelay.Common.Exceptions;
using OutboxRelay.Infrastructure.Models;

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
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaction> UpdateStatusAsync(Guid id, short status)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                throw new TransactionNotFoundException();
            }

            transaction.Status = status;
            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}
