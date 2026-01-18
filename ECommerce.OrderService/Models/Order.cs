namespace ECommerce.OrderService.Model {
    public class Order
    {
        public Guid Id { get; set; }           // Unique identifier
        public Guid UserId { get; set; }        // Reference to the user who placed the order
        public string Product { get; set; }    // Product name
        public int Quantity { get; set; }      // Number of items
        public decimal Price { get; set; }     // Total price
    }
}
