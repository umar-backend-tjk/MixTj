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

public class NewsService
    (DataContext context, 
    IMapper mapper, 
    IHttpContextAccessor accessor, 
    ICacheService cacheService) : INewsService
{
    private async Task RefreshCacheAsync()
    {
        try
        {
            var allNews = await context.News.Where(x => !x.IsDeleted).ToListAsync();
            await cacheService.AddAsync(CacheKeys.News, allNews, DateTimeOffset.Now.AddMinutes(5));
            Log.Information("Refreshed cache with key {k}", CacheKeys.News);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh cache with key {k}", CacheKeys.News);
        }
    }
    
    public async Task<Response<string>> CreateNewsAsync(CreateNewsDto dto)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
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
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in CreateNewsAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    public async Task<PaginationResponse<List<GetNewsDto>>> GetAllNewsAsync(NewsFilter filter)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            Log.Information("User {userId} tries to get all the news", userId);
            
            var newsInCache = await cacheService.GetAsync<List<News>>(CacheKeys.News);

            if (newsInCache is null)
            {
                newsInCache = await context.News
                    .Where(n => !n.IsDeleted)
                    .ToListAsync();

                await cacheService.AddAsync(CacheKeys.News, newsInCache, DateTimeOffset.Now.AddMinutes(5));
            }

            var query = newsInCache.AsQueryable();

            if (!string.IsNullOrEmpty(filter.AuthorId))
                query = query.Where(n => n.AuthorId == filter.AuthorId && !n.IsDeleted);
            
            if (!string.IsNullOrEmpty(filter.Title))
                query = query.Where(n => n.Title == filter.Title && !n.IsDeleted);
            
            if (filter.Category.HasValue)
                query = query.Where(n => n.Category == filter.Category && !n.IsDeleted);

            if (filter.Tags?.Length > 0)
                query = query.Where(n => n.Tags != null && n.Tags.Any(t => filter.Tags.Contains(t)));

            var totalCount = query.Count();
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var items = query
                .Skip(skip)
                .Take(filter.PageSize)
                .ToList();

            var mappedList = mapper.Map<List<GetNewsDto>>(items);

            Log.Information("Found {count} elements", totalCount);
            return new PaginationResponse<List<GetNewsDto>>(mappedList, totalCount, filter.PageNumber, filter.PageSize);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in GetAllNewsAsync");
            return new PaginationResponse<List<GetNewsDto>>(new List<GetNewsDto>(), 0, filter.PageNumber, filter.PageSize);
        }
    }

    public async Task<Response<GetNewsDto>> GetNewsByIdAsync(Guid id)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            Log.Information("User {userId} tries to get the news with id {newId}", userId, id);

            var news = await context.News.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

            if (news is null)
            {
                Log.Warning("Not found the news with id {newId}", id);
                return new Response<GetNewsDto>(HttpStatusCode.NotFound, "Not found the news");
            }

            var mappedNews = mapper.Map<GetNewsDto>(news);
            
            Log.Information("Got the news {newId} successfully", id);
            return new Response<GetNewsDto>(mappedNews);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in GetNewsByIdAsync");
            return new Response<GetNewsDto>(HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    public async Task<Response<string>> UpdateNewsAsync(UpdateNewsDto dto)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            Log.Information("User {userId} tries to update the news with id {newId}", userId, dto.Id);
            
            var news = await context.News.FirstOrDefaultAsync(n => n.Id == dto.Id && !n.IsDeleted);

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
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to update the news");
            }

            await RefreshCacheAsync();
            
            Log.Information("Updated a news {title} successfully", dto.Title);
            return new Response<string>(HttpStatusCode.OK, $"Updated a news {dto.Title} successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in UpdateNewsAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    public async Task<Response<string>> DeleteNewsAsync(Guid id)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Log.Information("User {userID} tries to delete the news with id {id}", userId, id);
            
            var theNews = await context.News.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            
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
            return new Response<string>(HttpStatusCode.OK, "Deleted the news successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in DeleteNewsAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }
}
