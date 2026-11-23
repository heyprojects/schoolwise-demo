using AutoMapper;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Application.Schools;

public sealed class SchoolMappingProfile : Profile
{
    public SchoolMappingProfile()
    {
        CreateMap<SchoolPhoto, SchoolPhotoDto>();
        CreateMap<School, SchoolDto>()
            .ForMember(destination => destination.Photos, options => options.MapFrom(source => source.Photos.OrderBy(photo => photo.SortOrder)));
        CreateMap<School, SchoolListItemDto>();
    }
}
