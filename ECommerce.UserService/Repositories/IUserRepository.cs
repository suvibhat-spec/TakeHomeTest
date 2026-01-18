

using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;

namespace ECommerce.UserService.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserAsync(Guid id, CancellationToken ct);
        Task<User> CreateUserAsync(CreateUserRequestDto user, CancellationToken ct);

        Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken ct);
    }
}