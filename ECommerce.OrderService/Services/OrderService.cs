using AutoMapper;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Repositories;
using ECommerce.Shared.Kafka;
using ECommerce.Shared.Kafka.Events;
using ECommerce.Shared.Kafka.Producer;


namespace ECommerce.OrderService.Service {

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IMapper _mapper;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ILogger<IOrderService> _logger;

        public OrderService(
            IOrderRepository repository,
            IMapper mapper,
            IKafkaProducer kafkaProducer,
            ILogger<IOrderService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _kafkaProducer = kafkaProducer;
            _logger = logger;
        }

        public async Task<OrderResponseDto?> GetOrderAsync(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Fetching order with ID: {OrderId}", id);
            var order = await _repository.GetOrderAsync(id, ct);
            
            if (order is null)
            {
                _logger.LogWarning("Order with ID: {OrderId} not found", id);
                return null;
            }

            var orderDto = _mapper.Map<OrderResponseDto>(order);
            _logger.LogInformation("Order with ID: {OrderId} fetched successfully", id);
            return orderDto;
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Fetching all orders");
            var orders = await _repository.GetAllOrdersAsync(ct);
            var orderDtos = _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
            _logger.LogInformation("All orders fetched successfully, count: {OrderCount}", orderDtos.Count());
            return orderDtos;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequestDto createOrderDto, CancellationToken ct)
        {
            _logger.LogInformation("Creating order for User ID: {UserId}, Product: {Product}, Quantity: {Quantity}, Price: {Price}", 
                createOrderDto.UserId, createOrderDto.Product, createOrderDto.Quantity, createOrderDto.Price);

            // Create order in repository
            var createdOrder = await _repository.CreateOrderAsync(createOrderDto, ct);
            _logger.LogInformation("Order created successfully with ID: {OrderId}", createdOrder.Id);

            // Publish OrderCreatedEvent to Kafka
            try
            {
                await _kafkaProducer.PublishAsync<OrderCreatedEvent>(
                    TopicConstants.OrderCreated,
                    createdOrder.Id.ToString(),
                    new OrderCreatedEvent(createdOrder.Id, createdOrder.UserId, createdOrder.Product, createdOrder.Quantity, createdOrder.Price)
                );
                _logger.LogInformation("OrderCreatedEvent published for order ID: {OrderId}", createdOrder.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreatedEvent for order ID: {OrderId}", createdOrder.Id);
                // Note: We don't re-throw here - order was created successfully, event publishing is not critical
            }

            var orderDto = _mapper.Map<OrderResponseDto>(createdOrder);
            return orderDto;
        }
    }
}
