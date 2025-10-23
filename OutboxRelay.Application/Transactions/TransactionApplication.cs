using OutboxRelay.Infrastructure.Models;
using OutboxRelay.Infrastructure.Repositories.Outboxes;
using OutboxRelay.Infrastructure.Repositories.Transactions;

namespace OutboxRelay.Application.Transactions
{
    public class TransactionApplication : ITransactionApplication
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly AppDbContext _context;

        public TransactionApplication(
            ITransactionRepository transactionRepository,
            IOutboxRepository outboxRepository,
            AppDbContext context)
        {
            _transactionRepository = transactionRepository;
            _outboxRepository = outboxRepository;
            _context = context;
        }


    }
}
