
using AutoMapper;
using EventTicketing.Core.DTOs.Events;
using EventTicketing.Core.DTOs.Halls;
using EventTicketing.Core.Entities;
using EventTicketing.Service.Helpers;

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
                    src => src.Hall.Name))
            .ForMember(
                dest => dest.StartTime,
                opt => opt.MapFrom(
                    src => IsraelTimeHelper.ToIsrael(
                        src.StartTime)))
            .ForMember(
                dest => dest.EndTime,
                opt => opt.MapFrom(
                    src => IsraelTimeHelper.ToIsrael(
                        src.EndTime)));
    }
}
