using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.DTOs.Like;
using Domain.DTOs.News;
using Domain.Entities;
using Domain.Filters;
using Infrastructure.Caching;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class NewsService(
    DataContext context,
    UserManager<AppUser> userManager,
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

            var query = newsInCache.Where(n => !n.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(filter.AuthorId))
                query = query.Where(n => n.AuthorId == filter.AuthorId);

            if (!string.IsNullOrEmpty(filter.Title))
                query = query.Where(n => n.Title == filter.Title);

            if (filter.Category.HasValue)
                query = query.Where(n => n.Category == filter.Category);

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
            return new PaginationResponse<List<GetNewsDto>>(new List<GetNewsDto>(), 0, filter.PageNumber,
                filter.PageSize);
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
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Log.Information("User {userId} tries to update the news with id {newsId}", userId, dto.Id);

            var currentUserRoles = await userManager.GetRolesAsync(await context.Users.FindAsync(userId)!);

            var news = await context.News.FirstOrDefaultAsync(n => n.Id == dto.Id && !n.IsDeleted);
            if (news == null)
            {
                Log.Warning("News with id {newsId} not found to update", dto.Id);
                return new Response<string>(HttpStatusCode.NotFound, "News not found");
            }

            var isOwner = news.AuthorId == userId;
            var isAdmin = currentUserRoles.Contains("Admin");
            var isModerator = currentUserRoles.Contains("Moderator");

            if (!isOwner && !(isAdmin || isModerator))
                return new Response<string>(HttpStatusCode.Forbidden, "Forbidden");

            mapper.Map(dto, news);
            news.UpdatedAt = DateTime.UtcNow;

            var result = await context.SaveChangesAsync();
            if (result == 0)
            {
                Log.Warning("Failed to update the news {title}", dto.Title);
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to update the news");
            }

            await RefreshCacheAsync();

            Log.Information("Updated news {title} successfully", dto.Title);
            return new Response<string>(HttpStatusCode.OK, $"Updated news {dto.Title} successfully");
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

            Log.Information("User {userId} tries to delete the news with id {newsId}", userId, id);

            var currentUserRoles = await userManager.GetRolesAsync(await context.Users.FindAsync(userId)!);

            var news = await context.News.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (news == null)
            {
                Log.Warning("News with id {newsId} not found for deletion", id);
                return new Response<string>(HttpStatusCode.NotFound, "News not found");
            }

            var isOwner = news.AuthorId == userId;
            var isAdmin = currentUserRoles.Contains("Admin");
            var isModerator = currentUserRoles.Contains("Moderator");

            if (!isOwner && !(isAdmin || isModerator))
                return new Response<string>(HttpStatusCode.Forbidden, "Forbidden");

            news.IsDeleted = true;
            var result = await context.SaveChangesAsync();
            if (result == 0)
            {
                Log.Warning("Failed to delete the news with id {newsId}", id);
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to delete the news");
            }

            await RefreshCacheAsync();

            Log.Information("Deleted news with id {newsId} successfully", id);
            return new Response<string>(HttpStatusCode.OK, "Deleted news successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in DeleteNewsAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    public async Task<Response<string>> AddLikeAsync(AddLikeDto dto)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             
            var mapped = mapper.Map<Like>(dto);
            mapped.UserId = userId!;
            
            var existingNews = await context.News
                .Include(n => n.Likes)
                .FirstOrDefaultAsync(n => n.Id == dto.TargetId && !n.IsDeleted);

            if (existingNews == null)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "News not found");
            }
            
            var exists = await context.Likes
                .AnyAsync(l => l.UserId == userId && l.TargetId == dto.TargetId);

            if (exists)
                return new Response<string>(HttpStatusCode.BadRequest, "Already liked");
             
            existingNews.Likes.Add(mapped);
            var result = await context.SaveChangesAsync();

            if (result == 0)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to add a like");
            }

            return new Response<string>(HttpStatusCode.OK, "Added a like");
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error in method AddLikeAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected error in method AddLikeAsync");
        }
    }

    public async Task<Response<List<GetLikeDto>>> GetAllLikes(Guid targetId)
    {
        try
        {
            var existingNews = await context.News
                .Include(n => n.Likes)
                .FirstOrDefaultAsync(n => n.Id == targetId && !n.IsDeleted);

            if (existingNews == null)
            {
                return new Response<List<GetLikeDto>>(HttpStatusCode.BadRequest, "News not found");
            }

            var likes = existingNews.Likes;

            var mapped = mapper.Map<List<GetLikeDto>>(likes);

            return new Response<List<GetLikeDto>>(mapped);
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error in method GetAllLikes");
            return new Response<List<GetLikeDto>>(HttpStatusCode.InternalServerError, "Unexpected error in method AddLikeAsync");
        }
    }

    public async Task<Response<string>> RemoveLike(Guid targetId)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var existingNews = await context.News
                .Include(n => n.Likes)
                .FirstOrDefaultAsync(n => n.Id == targetId && !n.IsDeleted);
            
            if (existingNews == null)
                return new Response<string>(HttpStatusCode.NotFound, "News not found");

            var like = existingNews.Likes.FirstOrDefault(l => l.UserId == userId);

            if (like == null)
                return new Response<string>(HttpStatusCode.NotFound, "Like not found");

            existingNews.Likes.Remove(like);

            var result = await context.SaveChangesAsync();

            if (result == 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to remove like");
            
            return new Response<string>(HttpStatusCode.OK, "Removed like");
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error in method RemoveLike");
            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected error in method AddLikeAsync");
        }
    }
}