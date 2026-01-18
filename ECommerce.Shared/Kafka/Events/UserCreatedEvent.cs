namespace ECommerce.Shared.Kafka.Events
{
    public sealed record UserCreatedEvent(Guid UserId, string Name, string Email);
}
