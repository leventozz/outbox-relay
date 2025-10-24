namespace OutboxRelay.Common.Exceptions
{
    public abstract class TransactionException : Exception
    {
        protected TransactionException(string message) : base(message) { }
        protected TransactionException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class TransactionNotFoundException : TransactionException
    {
        public TransactionNotFoundException(Guid transactionId)
            : base($"Transaction with ID '{transactionId}' was not found.") { }

        public TransactionNotFoundException(string message) : base(message) { }
    }
}
