using AutoMapper;
using Domain.DTOs.Auth;
using Domain.DTOs.User;
using Domain.Entities;

namespace Infrastructure.Profiles;

public class AppProfile : Profile
{
    public AppProfile()
    {
        CreateMap<RegisterDto, AppUser>();
        CreateMap<AppUser, GetUserDto>();
        CreateMap<UpdateUserDto, AppUser>();
    }
}