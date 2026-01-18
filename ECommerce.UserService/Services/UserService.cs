using AutoMapper;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;
using ECommerce.UserService.Repositories;
using ECommerce.Shared.Kafka.Producer;
using ECommerce.Shared.Kafka.Events;
using ECommerce.Shared.Kafka;

namespace ECommerce.UserService.Services {

    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ILogger<IUserService> _logger;

        public UserService(
            IUserRepository repository,
            IMapper mapper,
            IKafkaProducer kafkaProducer,
            ILogger<IUserService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _kafkaProducer = kafkaProducer;
            _logger = logger;
        }

        public async Task<UserResponseDto?> GetUserAsync(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Fetching user with ID: {UserId}", id);
            var user = await _repository.GetUserAsync(id, ct);
            
            if (user is null)
            {
                _logger.LogWarning("User with ID: {UserId} not found", id);
                return null;
            }

            var userDto = _mapper.Map<UserResponseDto>(user);
            _logger.LogInformation("User with ID: {UserId} fetched successfully", id);
            return userDto;
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto createUserDto, CancellationToken ct)
        {
            _logger.LogInformation("Creating user with Name: {UserName}, Email: {UserEmail}", 
                createUserDto.Name, createUserDto.Email);

            // Map DTO to entity
            var user = _mapper.Map<User>(createUserDto);

            // Create user in repository
            var createdUser = await _repository.CreateUserAsync(createUserDto, ct);
            _logger.LogInformation("User created successfully with ID: {UserId}", createdUser.Id);

            // Publish UserCreatedEvent to Kafka
            try
            {
                await _kafkaProducer.PublishAsync<UserCreatedEvent>(
                    TopicConstants.UserCreated,
                    createdUser.Id.ToString(),
                    new UserCreatedEvent(createdUser.Id, createdUser.Name, createdUser.Email)
                );
                _logger.LogInformation("UserCreatedEvent published for user ID: {UserId}", createdUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish UserCreatedEvent for user ID: {UserId}", createdUser.Id);
                // Note: We don't re-throw here - user was created successfully, event publishing is not critical
            }

            // Map entity to response DTO
            var responseDto = _mapper.Map<UserResponseDto>(createdUser);
            return responseDto;
        }
    }
}