namespace OutboxRelay.Common.Exceptions
{
    public abstract class TransactionException : Exception
    {

    }

    public class TransactionNotFoundException : TransactionException { }
}
