using AutoMapper;
using ECommerce.OrderService.Configuration;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Repositories;
using ECommerce.Shared.Kafka.Configuration;
using ECommerce.Shared.Kafka.Events;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.Extensions.Options;


namespace ECommerce.OrderService.Service {

    public class OrderService(IOrderRepository repository,
            IMapper mapper,
            IKafkaProducer kafkaProducer,
            ILogger<IOrderService> logger,
            HttpClient httpClient,
            IOptions<KafkaTopicSettings> topicSettings,
            IOptions<ServiceUrlSettings> serviceUrlSettings) : IOrderService
    {
        public async Task<OrderResponseDto?> GetOrderAsync(Guid id, CancellationToken ct)
        {
            logger.LogInformation("Fetching order with ID: {OrderId}", id);
            var order = await repository.GetOrderAsync(id, ct);
            
            if (order is null)
            {
                logger.LogWarning("Order with ID: {OrderId} not found", id);
                return null;
            }

            var orderDto = mapper.Map<OrderResponseDto>(order);
            logger.LogInformation("Order with ID: {OrderId} fetched successfully", id);
            return orderDto;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequestDto createOrderDto, CancellationToken ct)
        {
            logger.LogInformation("Creating order for User ID: {UserId}, Product: {Product}, Quantity: {Quantity}, Price: {Price}", 
                createOrderDto.UserId, createOrderDto.Product, createOrderDto.Quantity, createOrderDto.Price);

            // Validate user exists by checking local cache first, then UserService
            // this way service is more resilient to kafka downtime
            bool userExists = await ValidateUserExistsAsync(createOrderDto.UserId, ct);
            if (!userExists)
            {
                logger.LogError("Attempted to create order for non-existent User ID: {UserId}", createOrderDto.UserId);
                throw new ArgumentException("User does not exist");
            }
            // Create order in repository
            var createdOrder = await repository.CreateOrderAsync(createOrderDto, ct);
            logger.LogInformation("Order created successfully with ID: {OrderId}", createdOrder.Id);

            // Publish OrderCreatedEvent to Kafka
            try
            {
                await kafkaProducer.PublishAsync<OrderCreatedEvent>(
                    topicSettings.Value.OrderCreated,
                    createdOrder.Id.ToString(),
                    new OrderCreatedEvent(createdOrder.Id, createdOrder.UserId, createdOrder.Product ?? string.Empty, createdOrder.Quantity, createdOrder.Price)
                );
                logger.LogInformation("OrderCreatedEvent published for order ID: {OrderId}", createdOrder.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish OrderCreatedEvent for order ID: {OrderId}", createdOrder.Id);
                // Note: We don't re-throw here - order was created successfully, event publishing is not critical
            }

            var orderDto = mapper.Map<OrderResponseDto>(createdOrder);
            return orderDto;
        }

        private async Task<bool> ValidateUserExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                // First check local cache (UserRefs table populated by Kafka events)
                var localUserExists = await repository.IsUserExistAsync(userId, cancellationToken);
                if (localUserExists)
                {
                    logger.LogDebug("User {UserId} found in local cache", userId);
                    return true;
                }

                // If not in local cache, query UserService directly
                var userServiceUrl = serviceUrlSettings.Value.UserService;
                logger.LogInformation("User {UserId} not in cache, querying UserService at {Url}", userId, userServiceUrl);
                
                var response = await httpClient.GetAsync($"{userServiceUrl}/api/users/{userId}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("User {UserId} verified from UserService", userId);
                    return true;
                }
                
                logger.LogWarning("User {UserId} not found in UserService (Status: {StatusCode})", userId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating user {UserId} from UserService", userId);
                // since both cache and service call failed, assume user does not exist
                return false;
            }
        }
    }
}
