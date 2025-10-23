namespace OutboxRelay.Common.Exceptions
{
    public abstract class OutboxException : Exception
    {
    }

    public class OutboxNotFoundException : OutboxException { }
}
