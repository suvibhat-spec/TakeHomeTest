using AutoMapper;
using ECommerce.OrderService.Dto;
using ECommerce.OrderService.Model;

namespace ECommerce.OrderService.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Map from SourceModel to DestinationModel
            CreateMap<Order, OrderResponseDto>();
            CreateMap<CreateOrderRequestDto, Order>();
        }
    }
}
