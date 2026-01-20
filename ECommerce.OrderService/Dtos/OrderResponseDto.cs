namespace ECommerce.OrderService.Dto {
    public class OrderResponseDto
    {
        public Guid Id { get; set; }      

        public Guid UserId { get; set; }        // Reference to the user who placed the order

        public string Product { get; set; } = string.Empty;    // Product name

        public int Quantity { get; set; }      // Number of items
        public decimal Price { get; set; }     // Total price
    }
}