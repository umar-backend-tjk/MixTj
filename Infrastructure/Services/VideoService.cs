using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.DTOs.Like;
using Domain.DTOs.Video;
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

public class VideoService(
    DataContext context,
    UserManager<AppUser> userManager,
    IMapper mapper,
    IHttpContextAccessor accessor,
    ICacheService cacheService,
    IFileStorageService fileService) : IVideoService

{
    public async Task<Response<string>> CreateVideoAsync(CreateVideoDto createDto)
    {
        string? savedFilePath = null;
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            Log.Information("User {userId} tries to upload a new video", userId);

            var mapped = mapper.Map<Video>(createDto);
            mapped.AuthorId = userId;

            if (Path.GetExtension(createDto.VideoFile.FileName).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Saved the video to wwwroot/videos");
                savedFilePath = await fileService.SaveFileAsync(createDto.VideoFile, "videos");
                mapped.VideoPath = savedFilePath;
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

            Log.Information("Video created successfully. Title={Title}, User={UserId}", createDto.Title, userId);
            return new Response<string>(HttpStatusCode.OK, "Created a video successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in CreateVideoAsync");

            if (savedFilePath != null)
            {
                try
                {
                    await fileService.DeleteFileAsync(savedFilePath);
                    Log.Warning("Rollback: deleted uploaded video file {path}", savedFilePath);
                }
                catch (Exception deleteEx)
                {
                    Log.Error(deleteEx, "Rollback failed: could not delete file {path}", savedFilePath);
                }
            }

            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected server error");
        }
    }

    public async Task<PaginationResponse<List<GetVideoDto>>> GetAllVideoAsync(VideoFilter filter)
    {
        try
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

            var query = videosInCache.Where(v => !v.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(filter.AuthorId))
                query = query.Where(n => n.AuthorId == filter.AuthorId);

            if (!string.IsNullOrEmpty(filter.Title))
                query = query.Where(n => n.Title == filter.Title);

            var totalCount = query.Count();
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var items = query
                .Skip(skip)
                .Take(filter.PageSize)
                .ToList();

            var mappedList = mapper.Map<List<GetVideoDto>>(items);

            Log.Information("Found {count} elements", totalCount);
            return new PaginationResponse<List<GetVideoDto>>(mappedList, totalCount, filter.PageNumber,
                filter.PageSize);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in GetAllVideoAsync");
            return new PaginationResponse<List<GetVideoDto>>(new List<GetVideoDto>(), 0, filter.PageNumber,
                filter.PageSize);
        }
    }

    public async Task<Response<GetVideoDto>> GetVideoByIdAsync(Guid videoId)
    {
        try
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
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in GetVideoByIdAsync");
            return new Response<GetVideoDto>(HttpStatusCode.InternalServerError, "Unexpected server error");
        }
    }

    public async Task<Response<string>> UpdateVideoAsync(UpdateVideoDto updateDto)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Log.Information("User {userId} tries to update the video with id {videoId}", userId, updateDto.Id);

            var video = await context.Videos.FirstOrDefaultAsync(v => v.Id == updateDto.Id && !v.IsDeleted);
            if (video is null)
            {
                Log.Warning("Video with id {videoId} not found to update", updateDto.Id);
                return new Response<string>(HttpStatusCode.NotFound, "Video not found");
            }

            var currentUserRoles = await userManager.GetRolesAsync(await context.Users.FindAsync(userId)!);
            var isOwner = video.AuthorId == userId;
            var isAdmin = currentUserRoles.Contains("Admin");
            var isModerator = currentUserRoles.Contains("Moderator");

            if (!isOwner && !(isAdmin || isModerator))
            {
                Log.Warning("User {userId} is not allowed to update video {videoId}", userId, updateDto.Id);
                return new Response<string>(HttpStatusCode.Forbidden, "Forbidden");
            }

            mapper.Map(updateDto, video);
            video.UpdatedAt = DateTime.UtcNow;

            var result = await context.SaveChangesAsync();
            if (result == 0)
            {
                Log.Warning("Failed to update the video {title}", updateDto.Title);
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to update the video");
            }

            await cacheService.RemoveAsync(CacheKeys.Videos);

            Log.Information("Updated video {title} successfully", updateDto.Title);
            return new Response<string>(HttpStatusCode.OK, $"Updated video {updateDto.Title} successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in UpdateVideoAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected server error");
        }
    }

    public async Task<Response<string>> DeleteVideoAsync(Guid videoId)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Log.Information("User {userId} tries to delete the video with id {videoId}", userId, videoId);

            var video = await context.Videos.FirstOrDefaultAsync(v => v.Id == videoId && !v.IsDeleted);
            if (video is null)
            {
                Log.Warning("Video with id {videoId} not found", videoId);
                return new Response<string>(HttpStatusCode.NotFound, "Video not found");
            }

            var currentUserRoles = await userManager.GetRolesAsync(await context.Users.FindAsync(userId)!);
            var isOwner = video.AuthorId == userId;
            var isAdmin = currentUserRoles.Contains("Admin");
            var isModerator = currentUserRoles.Contains("Moderator");

            if (!isOwner && !(isAdmin || isModerator))
            {
                Log.Warning("User {userId} is not allowed to delete video {videoId}", userId, videoId);
                return new Response<string>(HttpStatusCode.Forbidden, "Forbidden");
            }

            context.Videos.Remove(video);
            var result = await context.SaveChangesAsync();
            if (result == 0)
            {
                Log.Warning("Failed to delete the video with id {videoId}", videoId);
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to delete the video");
            }

            await cacheService.RemoveAsync(CacheKeys.Videos);

            if (!string.IsNullOrEmpty(video.VideoPath))
            {
                try
                {
                    await fileService.DeleteFileAsync(video.VideoPath);
                    Log.Information("Deleted video file {videoPath}", video.VideoPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete video file {videoPath}", video.VideoPath);
                }
            }

            Log.Information("Deleted video {videoId} successfully", videoId);
            return new Response<string>(HttpStatusCode.OK, "Deleted video successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in DeleteVideoAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected server error");
        }
    }

    public async Task<Response<string>> AddLikeAsync(AddLikeDto dto)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var mapped = mapper.Map<Like>(dto);
            mapped.UserId = userId!;

            var existingVideo = await context.Videos
                .Include(n => n.Likes)
                .FirstOrDefaultAsync(n => n.Id == dto.TargetId && !n.IsDeleted);

            if (existingVideo == null)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Video not found");
            }

            var exists = await context.Likes
                .AnyAsync(l => l.UserId == userId && l.TargetId == dto.TargetId);

            if (exists)
                return new Response<string>(HttpStatusCode.BadRequest, "Already liked");

            existingVideo.Likes.Add(mapped);
            var result = await context.SaveChangesAsync();

            if (result == 0)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to add a like");
            }

            return new Response<string>(HttpStatusCode.OK, "Added a like");
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error in method VideoService.AddLikeAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected error in method AddLikeAsync");
        }
    }

    public async Task<Response<List<GetLikeDto>>> GetAllLikes(Guid targetId)
    {
        try
        {
            var existingVideo = await context.Videos
                .Include(n => n.Likes)
                .FirstOrDefaultAsync(n => n.Id == targetId && !n.IsDeleted);

            if (existingVideo == null)
            {
                return new Response<List<GetLikeDto>>(HttpStatusCode.BadRequest, "Video not found");
            }

            var likes = existingVideo.Likes;

            var mapped = mapper.Map<List<GetLikeDto>>(likes);

            return new Response<List<GetLikeDto>>(mapped);
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error in method VideoService.GetAllLikes");
            return new Response<List<GetLikeDto>>(HttpStatusCode.InternalServerError,
                "Unexpected error in method AddLikeAsync");
        }
    }

    public async Task<Response<string>> RemoveLike(Guid targetId)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var existingVideo = await context.Videos
                .Include(n => n.Likes)
                .FirstOrDefaultAsync(n => n.Id == targetId && !n.IsDeleted);

            if (existingVideo == null)
                return new Response<string>(HttpStatusCode.NotFound, "Video not found");

            var like = existingVideo.Likes.FirstOrDefault(l => l.UserId == userId);

            if (like == null)
                return new Response<string>(HttpStatusCode.NotFound, "Like not found");

            existingVideo.Likes.Remove(like);
            context.Likes.Remove(like);

            var result = await context.SaveChangesAsync();

            if (result == 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to remove like");

            return new Response<string>(HttpStatusCode.OK, "Removed like");
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error in method VideoService.RemoveLike");
            return new Response<string>(HttpStatusCode.InternalServerError, "Unexpected error in method AddLikeAsync");
        }
    }
}