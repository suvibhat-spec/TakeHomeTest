namespace ECommerce.Shared.Kafka.Events
{
    public sealed record OrderCreatedEvent(Guid OrderId, Guid UserId, string Product, int Quantity, decimal Price);
}
