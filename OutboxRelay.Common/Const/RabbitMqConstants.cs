namespace OutboxRelay.Common.Const
{
    public class RabbitMqConstants
    {
        public const string TransactionExchangeName = "exchange.transactions";
        public const string TransactionQueueName = "queue.transactions.outbox";
        public const string TransactionCreateRoutingKey = "routing.transactions.create";
    }
}
