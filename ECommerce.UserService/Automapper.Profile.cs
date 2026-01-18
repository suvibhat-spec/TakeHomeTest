using AutoMapper;
using ECommerce.UserService.Dto;
using ECommerce.UserService.Model;

namespace ECommerce.UserService.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Map from SourceModel to DestinationModel
            CreateMap<User, UserResponseDto>();
            CreateMap<CreateUserRequestDto, User>();
        }
    }
}
