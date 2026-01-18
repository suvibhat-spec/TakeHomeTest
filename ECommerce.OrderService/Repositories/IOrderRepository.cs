

using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;

namespace ECommerce.OrderService.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderAsync(Guid id, CancellationToken ct);
        Task<Order> CreateOrderAsync(CreateOrderRequestDto order, CancellationToken ct);

        Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken ct);
    }
}