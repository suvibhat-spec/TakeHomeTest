using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Events;

namespace ECommerce.UserService.Kafka.Handlers
{
    public class OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger) : IEventHandler<OrderCreatedEvent>
    {
        public Task HandleAsync(OrderCreatedEvent incomingEvent, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[OrderCreatedEventHandler] Received order: OrderId={incomingEvent.OrderId}, UserId={incomingEvent.UserId}, Product={incomingEvent.Product}, Quantity={incomingEvent.Quantity}, Price={incomingEvent.Price}");
            // TODO: Add business logic here to handle order created event
            return Task.CompletedTask;
        }
    }
}
