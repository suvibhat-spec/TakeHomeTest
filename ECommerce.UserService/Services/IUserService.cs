using ECommerce.UserService.Dto;

namespace ECommerce.UserService.Services 
{
    public interface IUserService
    {
        Task<UserResponseDto?> GetUserAsync(Guid id, CancellationToken ct);
        Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto createUserDto, CancellationToken ct);
    }
}