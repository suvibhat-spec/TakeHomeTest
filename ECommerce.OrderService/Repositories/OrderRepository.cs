using ECommerce.OrderService.Configuration;
using ECommerce.OrderService.Data;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace ECommerce.OrderService.Repositories   
{
    public class OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger) : IOrderRepository
    {
        public async Task<Order> CreateOrderAsync(CreateOrderRequestDto order, CancellationToken cancellationToken)
        {   
            logger.LogInformation("Creating order for User ID: {UserId}, Product: {Product}, Quantity: {Quantity}, Price: {Price}", 
                order.UserId, order.Product, order.Quantity, order.Price);
            Order addOrder = new Order();
            addOrder.Id = Guid.NewGuid();
            addOrder.Price = order.Price;
            addOrder.Product = order.Product;
            addOrder.Quantity = order.Quantity;
            addOrder.UserId = order.UserId;

            context.Orders.Add(addOrder);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Order created with ID: {OrderId}", addOrder.Id);
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

        public async Task<bool> IsUserExistAsync(Guid userId, CancellationToken ct)
        {
            return await context.UserRefs.AnyAsync(u => u.Id == userId, ct);
        }
    }
}