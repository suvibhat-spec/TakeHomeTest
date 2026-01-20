using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ECommerce.OrderService.Dto {
    public class CreateOrderRequestDto {
        [Required]
        public Guid UserId { get; set; }        // Reference to the user who placed the order
        [Required]
        public string Product { get; set; } = string.Empty;    // Product name
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }      // Number of items
        [RegularExpression(@"^\d+(\.\d{1,2})?$")]   // two decimal places
        [Range(0, 999999999.99)]
        public decimal Price { get; set; }     // Total price
    }
}