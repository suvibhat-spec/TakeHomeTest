namespace ECommerce.Shared.Kafka.Consumer
{
    public interface IEventHandler<TEvent>
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
    }
}