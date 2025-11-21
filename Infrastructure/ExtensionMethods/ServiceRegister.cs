using Infrastructure.Caching;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.ExtensionMethods;

public static class ServiceRegister
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<ICommentService, CommentService>();
    }
}