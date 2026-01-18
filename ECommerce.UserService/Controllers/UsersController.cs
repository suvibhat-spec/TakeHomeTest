using ECommerce.UserService.Dto;
using ECommerce.UserService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.UserService.Controller;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser([FromRoute] Guid id, CancellationToken ct)
    {
        logger.LogInformation("Retrieving user with ID: {UserId}", id);

        var user = await userService.GetUserAsync(id, ct);
        if (user is null)
        {
            logger.LogWarning("User with ID: {UserId} not found", id);
            return NotFound(id);
        }

        logger.LogInformation("User with ID: {UserId} retrieved successfully", id);
        return Ok(user);
    }


    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> AddUser([FromBody] CreateUserRequestDto userDto, CancellationToken ct)
    {
        logger.LogInformation("Creating a new user with Name: {UserName}, Email: {UserEmail}", userDto.Name, userDto.Email);

        var createdUser = await userService.CreateUserAsync(userDto, ct);
        if (createdUser is null)
        {
            logger.LogError("Failed to create user with Name: {UserName}, Email: {UserEmail}", userDto.Name, userDto.Email);
            return BadRequest("User creation failed");
        }
        logger.LogInformation("User with ID: {UserId} created successfully", createdUser.Id);
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser); 
    }
}
