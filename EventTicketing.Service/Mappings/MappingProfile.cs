
using AutoMapper;
using EventTicketing.Core.DTOs.Events;
using EventTicketing.Core.DTOs.Halls;
using EventTicketing.Core.Entities;

namespace EventTicketing.Service.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Hall, HallResponseDto>();

        CreateMap<Event, EventResponseDto>()
            .ForMember(
                dest => dest.HallName,
                opt => opt.MapFrom(
                    src => src.Hall.Name));
    }
}
