namespace OutboxRelay.Common.Exceptions
{
    public abstract class OutboxException : Exception
    {
        protected OutboxException(string message) : base(message) { }
        protected OutboxException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class OutboxNotFoundException : OutboxException
    {
        public OutboxNotFoundException(Guid outboxId)
            : base($"Outbox message with ID '{outboxId}' was not found.") { }

        public OutboxNotFoundException(string message) : base(message) { }
    }
}
