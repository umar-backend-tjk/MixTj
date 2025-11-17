using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.DTOs.News;
using Domain.Entities;
using Domain.Filters;
using Infrastructure.Caching;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class NewsService(DataContext context, IMapper mapper, IHttpContextAccessor accessor, ICacheService cacheService) : INewsService
{
    private async Task RefreshCacheAsync()
    {
        var allNews = await context.News.ToListAsync();
        await cacheService.AddAsync(CacheKeys.News, allNews, DateTimeOffset.Now.AddMinutes(5));
        Log.Information("Refreshed cache with key {k}", CacheKeys.News);
    }
    
    public async Task<Response<string>> CreateNewsAsync(CreateNewsDto dto)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to create a new news", userId);
        
        var mapped = mapper.Map<News>(dto);
        mapped.AuthorId = userId;
        
        await context.News.AddAsync(mapped);
        var result = await context.SaveChangesAsync();

        if (result == 0)
        {
            Log.Warning("Failed to create a news {title}", dto.Title);
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to create a new news");
        }

        await RefreshCacheAsync();
        
        Log.Information("Created a news {title} successfully", dto.Title);
        return new Response<string>(HttpStatusCode.OK, $"Created a news {dto.Title} successfully");
    }

    public async Task<PaginationResponse<List<GetNewsDto>>> GetAllNewsAsync(NewsFilter filter)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to get all the news", userId);
        
        var newsInCache = await cacheService.GetAsync<List<News>>(CacheKeys.News);

        if (newsInCache is null)
        {
            var newsList = await context.News.ToListAsync();
            
            var expirationTime = DateTimeOffset.Now.AddMinutes(5);
            await cacheService.AddAsync(CacheKeys.News, newsList, expirationTime);
            
            var mapped = mapper.Map<List<GetNewsDto>>(newsList);
            return new PaginationResponse<List<GetNewsDto>>(mapped);
        }

        var mappedFromCache = mapper.Map<List<GetNewsDto>>(newsInCache);
        
        Log.Information("Found {count} news items", newsInCache.Count);
        return new PaginationResponse<List<GetNewsDto>>(mappedFromCache);
    }

    public async Task<Response<GetNewsDto>> GetNewsByIdAsync(Guid id)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to get the news with id {newId}", userId, id);
        
        var news = await context.News.FindAsync(id);

        if (news is null)
        {
            Log.Warning("Not found the news with id {newId}", id);
            return new Response<GetNewsDto>(HttpStatusCode.NotFound, "Not found the news");
        }

        var mappedNews = mapper.Map<GetNewsDto>(news);
        
        Log.Information("Got the news {newId} successfully", id);
        return new Response<GetNewsDto>(mappedNews);
    }

    public async Task<Response<string>> UpdateNewsAsync(UpdateNewsDto dto)
    {
        var userId = accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to update the news with id {newId}", userId, dto.Id);
        
        var news = await context.News.FindAsync(dto.Id);

        if (news is null)
        {
            Log.Warning("Not found the news with id {newId} to update", dto.Id);
            return new Response<string>(HttpStatusCode.NotFound, "Not found the news");
        }

        mapper.Map(dto, news);
        news.UpdatedAt = DateTime.UtcNow;
        
        var result = await context.SaveChangesAsync();
        
        if (result == 0)
        {
            Log.Warning("Failed to update the news {title}", dto.Title);
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to update the new news");
        }

        await RefreshCacheAsync();
        
        Log.Information("Updated a news {title} successfully", dto.Title);
        return new Response<string>(HttpStatusCode.OK, $"Updated a news {dto.Title} successfully");
    }

    public async Task<Response<string>> DeleteNewsAsync(Guid id)
    {
        var userId = accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log.Information("User {userID} tries to delete the news with id {id}", userId, id);
        
        var theNews = await context.News.FindAsync(id);
        
        if (theNews is null)
        {
            Log.Warning("Not found the news with id {id}", id);
            return new Response<string>(HttpStatusCode.NotFound, "News not found");
        }

        theNews.IsDeleted = true;
        var result = await context.SaveChangesAsync();

        if (result == 0)
        {
            Log.Warning("Failed to delete the news with id {id}", id);
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to delete the news");
        }
        
        await RefreshCacheAsync();
        
        Log.Information("User {userId} deleted the news with id {id} successfully", userId, id);
        return new Response<string>(HttpStatusCode.OK, "Deleted  the news successfully");
    }
}