using AutoMapper;
using Domain.DTOs.Auth;
using Domain.DTOs.News;
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

        CreateMap<CreateNewsDto, News>();
        CreateMap<UpdateNewsDto, News>();
        CreateMap<News, GetNewsDto>();
    }
}