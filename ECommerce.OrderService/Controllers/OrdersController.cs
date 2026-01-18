using AutoMapper;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;
using ECommerce.OrderService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IOrderRepository orderRepository, IMapper mapper, ILogger<OrdersController> logger)  : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder([FromRoute] Guid id, CancellationToken ct)
        {
            logger.LogInformation("Retrieving order with ID: {OrderId}", id);

            var order = await orderRepository.GetOrderAsync(id, ct);
            var orderDto = mapper.Map<OrderResponseDto>(order);
            logger.LogInformation("Order with ID: {OrderId} retrieved successfully", id);
            return order is null ? NotFound(id):  Ok(orderDto);
        }

        [HttpPost]
        public async Task<ActionResult<Order>> AddOrder([FromBody] CreateOrderRequestDto order, CancellationToken ct)
        {
            logger.LogInformation("Creating a new order for User ID: {UserId}, Product: {Product}, Quantity: {Quantity}, Price: {Price}", order.UserId, order.Product, order.Quantity, order.Price);

            var createdOrder = await orderRepository.CreateOrderAsync(order, ct);
            var orderDto = mapper.Map<OrderResponseDto>(createdOrder);
            logger.LogInformation("Order with ID: {OrderId} created successfully", createdOrder.Id);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, orderDto);
        }
    }
}
