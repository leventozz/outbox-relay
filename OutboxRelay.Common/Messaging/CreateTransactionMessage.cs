namespace OutboxRelay.Common.Messaging
{
    public class CreateTransactionMessage
    {
        public Guid Id { get; set; }
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
