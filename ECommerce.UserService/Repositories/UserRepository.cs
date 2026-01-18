using ECommerce.UserService.Data;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UserService.Repositories
{
    public class UserRepository(UserDbContext context, ILogger<UserRepository> logger) : IUserRepository
    {
        public async Task<User> CreateUserAsync(CreateUserRequestDto user, CancellationToken cancellationToken)
        {   // Manual uniqueness check
            if (await context.Users.AnyAsync(u => u.Email == user.Email))
            {
                logger.LogError("Attempted to create user with existing Email: {UserEmail}", user.Email);
                throw new InvalidOperationException("Email already exists");
            }
            User addUser = new User();
            addUser.Id = Guid.NewGuid();
            addUser.Name = user.Name;
            addUser.Email = user.Email;

            context.Users.Add(addUser);
            await context.SaveChangesAsync(cancellationToken);
            
            return addUser;
        }

        public async Task<User?> GetUserAsync(Guid id, CancellationToken cancellationToken)
        {
            return await context.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken ct)
        {
            return await context.Users.ToListAsync(ct);
        }
    }
}