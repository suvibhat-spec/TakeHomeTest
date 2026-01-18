using ECommerce.OrderService.Data;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Models;
using ECommerce.Shared.Kafka;
using ECommerce.Shared.Kafka.Consumer;
using ECommerce.Shared.Kafka.Events;

namespace ECommerce.OrderService.Kafka.Handlers
{
    public class UserCreatedEventHandler(OrderDbContext orderDbContext, ILogger<UserCreatedEventHandler> logger) : IEventHandler<UserCreatedEvent>
    {
        public Task HandleAsync(UserCreatedEvent incomingEvent, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[UserCreatedEventHandler] Received user: UserId={incomingEvent.UserId}, Name={incomingEvent.Name}");
            //  business logic to handle user created event
            // store user reference in OrderDbContext
            var userRef = new UserRef { Id = incomingEvent.UserId };
            orderDbContext.UserRefs.Add(userRef);
            orderDbContext.SaveChanges();
            return Task.CompletedTask;
        }
}
}
