namespace OutboxRelay.Application.Features.Consumers
{
    public interface IMessageHandler<T> where T : class
    {
        Task HandleAsync(T message, CancellationToken cancellationToken);
    }
}
