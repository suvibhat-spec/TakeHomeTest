using System.ComponentModel.DataAnnotations;

namespace ECommerce.UserService.Dto {
    public class CreateUserRequestDto
    {    
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public required string Name { get; set; } // User's full name
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }     // User's email address
    }
}