using AutoMapper;
using Domain.DTOs.Auth;
using Domain.Entities;

namespace Infrastructure.Profiles;

public class AppProfile : Profile
{
    public AppProfile()
    {
        CreateMap<RegisterDto, AppUser>();
    }
}