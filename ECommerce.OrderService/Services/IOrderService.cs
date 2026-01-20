using ECommerce.OrderService.Dto;

namespace ECommerce.OrderService.Service {

    public interface IOrderService
    {
        Task<OrderResponseDto?> GetOrderAsync(Guid id, CancellationToken ct);
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequestDto createOrderDto, CancellationToken ct);
    }
}
