using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.DTOs.Video;
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

public class VideoService(
    DataContext context, 
    IMapper mapper, 
    IHttpContextAccessor accessor, 
    ICacheService cacheService,
    IFileStorageService fileService) : IVideoService
    
{
    public async Task<Response<string>> CreateVideoAsync(CreateVideoDto createDto)
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to upload a new video", userId);
        
        var mapped = mapper.Map<Video>(createDto);
        mapped.AuthorId = userId;

        if (Path.GetExtension(createDto.VideoFile.FileName).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Saved the video to wwwroot/videos");
            mapped.VideoPath = await fileService.SaveFileAsync(createDto.VideoFile, "videos");
        }
        else
        {
            Log.Warning("Failed to upload a video: wrong format");
            return new Response<string>(HttpStatusCode.BadRequest, "Wrong format of video. Only .mp4 is allowed");
        }
        
        await context.Videos.AddAsync(mapped);
        var result = await context.SaveChangesAsync();

        if (result == 0)
        {
            Log.Warning("Failed to create a video {title}", createDto.Title);
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to create a new video");
        }

        await cacheService.RemoveAsync(CacheKeys.Videos);
        
        Log.Information("Created a video {title} successfully", createDto.Title);
        return new Response<string>(HttpStatusCode.OK, "Created a video successfully");
    }

    public async Task<PaginationResponse<List<GetVideoDto>>> GetAllVideoAsync(VideoFilter filter)
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to get {count} video", userId, filter.PageSize);
        
        var videosInCache = await cacheService.GetAsync<List<Video>>(CacheKeys.Videos);

        if (videosInCache is null)
        {
            videosInCache = await context.Videos
                .Where(v => !v.IsDeleted)
                .ToListAsync();

            await cacheService.AddAsync(CacheKeys.Videos, videosInCache, DateTimeOffset.Now.AddMinutes(5));
        }

        var query = videosInCache.AsQueryable();

        if (!string.IsNullOrEmpty(filter.AuthorId))
            query = query.Where(n => n.AuthorId == filter.AuthorId && !n.IsDeleted);
        
        if (!string.IsNullOrEmpty(filter.Title))
            query = query.Where(n => n.Title == filter.Title && !n.IsDeleted);

        var totalCount = query.Count();
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var items = query
            .Skip(skip)
            .Take(filter.PageSize)
            .ToList();

        var mappedList = mapper.Map<List<GetVideoDto>>(items);

        Log.Information("Found {count} elements", totalCount);
        return new PaginationResponse<List<GetVideoDto>>(mappedList, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<Response<GetVideoDto>> GetVideoByIdAsync(Guid videoId)
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to get the video with id {videoId}", userId, videoId);

        var video = await context.Videos.FirstOrDefaultAsync(n => n.Id == videoId && !n.IsDeleted);

        if (video is null)
        {
            Log.Warning("Not found the video with id {videoId}", videoId);
            return new Response<GetVideoDto>(HttpStatusCode.NotFound, "Not found the video");
        }

        var mappedVideo = mapper.Map<GetVideoDto>(video);
        
        Log.Information("Got the video {videoId} successfully", videoId);
        return new Response<GetVideoDto>(mappedVideo);
    }

    public async Task<Response<string>> UpdateVideoAsync(UpdateVideoDto updateDto)
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        Log.Information("User {userId} tries to update the video with id {videoId}", userId, updateDto.Id);
        
        var video = await context.Videos.FirstOrDefaultAsync(n => n.Id == updateDto.Id && !n.IsDeleted);

        if (video is null)
        {
            Log.Warning("Not found the video with id {videoId} to update", updateDto.Id);
            return new Response<string>(HttpStatusCode.NotFound, "Not found the video");
        }

        mapper.Map(updateDto, video);
        video.UpdatedAt = DateTime.UtcNow;
        
        var result = await context.SaveChangesAsync();
        
        if (result == 0)
        {
            Log.Warning("Failed to update the video {title}", video.Title);
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to update the new video");
        }

        await cacheService.RemoveAsync(CacheKeys.Videos);
        
        Log.Information("Updated a video {title} successfully", updateDto.Title);
        return new Response<string>(HttpStatusCode.OK, $"Updated a video {updateDto.Title} successfully");
    }

    public async Task<Response<string>> DeleteVideoAsync(Guid videoId)
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Log.Information("User {userID} tries to delete the video with id {id}", userId, videoId);
        
        var theVideo = await context.Videos.FirstOrDefaultAsync(n => n.Id == videoId && !n.IsDeleted);
        
        if (theVideo is null)
        {
            Log.Warning("Not found the video with id {id}", videoId);
            return new Response<string>(HttpStatusCode.NotFound, "Video not found");
        }

        theVideo.IsDeleted = true;
        var result = await context.SaveChangesAsync();

        if (result == 0)
        {
            Log.Warning("Failed to delete the video with id {id}", videoId);
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to delete the video");
        }
        
        await cacheService.RemoveAsync(CacheKeys.Videos);
        
        Log.Information("User {userId} deleted the video with id {id} successfully", userId, videoId);
        return new Response<string>(HttpStatusCode.OK, "Deleted the video successfully");
    }
}