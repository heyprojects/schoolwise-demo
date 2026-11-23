using AutoMapper;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Application.Auth;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
