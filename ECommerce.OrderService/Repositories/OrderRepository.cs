using ECommerce.OrderService.Data;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.OrderService.Repositories
{
    public class OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger) : IOrderRepository
    {
        public async Task<Order> CreateOrderAsync(CreateOrderRequestDto order, CancellationToken cancellationToken)
        {   
            bool userExists = await context.UserRefs.AnyAsync(u => u.Id == order.UserId); // ensure user exists
            if (!userExists)
            {
                logger.LogError("Attempted to create order for non-existent User ID: {UserId}", order.UserId);
                throw new ArgumentException("User does not exist");
            }

            Order addOrder = new Order();
            addOrder.Id = Guid.NewGuid();
            addOrder.Price = order.Price;
            addOrder.Product = order.Product;
            addOrder.Quantity = order.Quantity;
            addOrder.UserId = order.UserId;

            context.Orders.Add(addOrder);
            await context.SaveChangesAsync(cancellationToken);

            return addOrder;
        }

        public async Task<Order?> GetOrderAsync(Guid id, CancellationToken cancellationToken)
        {
            return await context.Orders.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken ct)
        {
            return await context.Orders.ToListAsync(ct);
        }
    }
}