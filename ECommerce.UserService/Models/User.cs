namespace ECommerce.UserService.Model {
    public class User
    {
        public Guid Id { get; set; }           // Unique identifier
        public string Name { get; set;}  = string.Empty;   // User's full name
        public string Email { get; set; } = string.Empty;      // User's email address
    }
}
