using AutoMapper;
using Domain.DTOs.Auth;
using Domain.DTOs.Comment;
using Domain.DTOs.News;
using Domain.DTOs.User;
using Domain.DTOs.Video;
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
        
        CreateMap<CreateVideoDto, Video>();
        CreateMap<UpdateVideoDto, Video>();
        CreateMap<Video, GetVideoDto>();
        
        CreateMap<CreateCommentDto, Comment>();
        CreateMap<UpdateCommentDto, Comment>();
        CreateMap<ContextMarshalException, GetCommentDto>();
    }
}