namespace ECommerce.Shared.Kafka
{
    /// <summary>
    /// Kafka topic names used across microservices
    /// </summary>
    public static class TopicConstants
    {
        public const string OrderCreated = "order.created";
        public const string UserCreated = "user.created";
        // Future events can be added here
        // public const string OrderUpdated = "order-updated";
        // public const string OrderCancelled = "order-cancelled";
    }
}