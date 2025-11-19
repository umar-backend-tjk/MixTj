using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.DTOs.Comment;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class CommentService(
    DataContext context, 
    UserManager<AppUser> userManager,
    IMapper mapper,
    IHttpContextAccessor accessor) : ICommentService
{
    public async Task<Response<string>> CreateCommentAsync(CreateCommentDto dto)
    {
        try
        {
            var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            Log.Information("User {userId} tries to post a comment to video/news with id {publication}", userId, dto.NewsId ?? dto.VideoId);

            if (dto.NewsId == null && dto.VideoId == null)
            {
                Log.Warning("Failed to post a comment: enter newsId or videoId");
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to post a comment: enter newsId or videoId");
            }
            
            var mapped = mapper.Map<Comment>(dto);
            mapped.UserId = userId;
            
            await context.Comments.AddAsync(mapped);
            var result = await context.SaveChangesAsync();

            if (result == 0)
            {
                Log.Warning("Failed to create a comment");
                return new Response<string>(HttpStatusCode.BadRequest, "Failed to create a new comment");
            }
            
            Log.Information("Created a comment successfully");
            return new Response<string>(HttpStatusCode.OK, $"Created a comment successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in CreateNewsAsync");
            return new Response<string>(HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    public async Task<Response<List<GetCommentDto>>> GetAllCommentAsync(Guid? videoId, Guid? newsId)
    {
        try
        {
            var query = context.Comments.Where(c => !c.IsDeleted).AsQueryable();

            if (videoId.HasValue && newsId.HasValue)
            {
                Log.Warning("Exactly one of NewsId or VideoId must be provided.");
                return new Response<List<GetCommentDto>>(HttpStatusCode.BadRequest,
                    "Exactly one of NewsId or VideoId must be provided.");
            }
            
            if (videoId.HasValue)
            {
                var existingVideo = await context.Videos.FirstOrDefaultAsync(v => v.Id == videoId && !v.IsDeleted);
                if (existingVideo == null)
                {
                    return new PaginationResponse<List<GetCommentDto>>(HttpStatusCode.BadRequest,
                        "Not found the video to comment it");
                }
                query = query.Where(c => c.VideoId == videoId);
            }
            
            if (newsId.HasValue)
            {
                var existingNews = await context.News.FirstOrDefaultAsync(n => n.Id == newsId && !n.IsDeleted);
                if (existingNews == null)
                {
                    return new PaginationResponse<List<GetCommentDto>>(HttpStatusCode.BadRequest,
                        "Not found the news to comment it");
                }
                query = query.Where(c => c.NewsId == newsId);
            }
            
            var totalCount = await query.CountAsync();

            if (totalCount < 1)
            {
                Log.Warning("Not found comments");
                return new Response<List<GetCommentDto>>(HttpStatusCode.NotFound, "Not found comments");
            }
            
            var comments = await query.ToListAsync();

            var mappedList = mapper.Map<List<GetCommentDto>>(comments);

            Log.Information("Found {count} comments", totalCount);
            return new Response<List<GetCommentDto>>(mappedList);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in GetAllVideoAsync");
            return new Response<List<GetCommentDto>>(HttpStatusCode.InternalServerError, "Failed to get comments");
        }
    }

    public async Task<Response<string>> UpdateCommentAsync(UpdateCommentDto dto)
{
    try
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var existingComment = await context.Comments.FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted);

        if (existingComment == null)
        {
            Log.Warning("Comment {commentId} does not exist", dto.Id);
            return new Response<string>(HttpStatusCode.NotFound, "Comment not found");
        }

        var currentUser = await context.Users.FindAsync(userId);
        var currentUserRoles = await userManager.GetRolesAsync(currentUser!);
        var isOwner = existingComment.UserId == userId;
        var isAdmin = currentUserRoles.Contains("Admin");
        var isModerator = currentUserRoles.Contains("Moderator");

        if (!isOwner && !(isAdmin || isModerator))
        {
            Log.Warning("User {userId} is not allowed to update comment {commentId}", userId, dto.Id);
            return new Response<string>(HttpStatusCode.Forbidden, "Forbidden");
        }

        mapper.Map(dto, existingComment);
        existingComment.UpdatedAt = DateTime.UtcNow;

        var result = await context.SaveChangesAsync();
        if (result == 0)
        {
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to update the comment");
        }

        return new Response<string>(HttpStatusCode.OK, "Updated comment successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unexpected error in UpdateCommentAsync");
        return new Response<string>(HttpStatusCode.InternalServerError, "Failed to update the comment");
    }
}

public async Task<Response<string>> DeleteCommentAsync(Guid id)
{
    try
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var existingComment = await context.Comments.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (existingComment == null)
        {
            Log.Warning("Comment {commentId} does not exist", id);
            return new Response<string>(HttpStatusCode.NotFound, "Comment not found");
        }

        var currentUser = await context.Users.FindAsync(userId);
        var currentUserRoles = await userManager.GetRolesAsync(currentUser!);
        var isOwner = existingComment.UserId == userId;
        var isAdmin = currentUserRoles.Contains("Admin");
        var isModerator = currentUserRoles.Contains("Moderator");

        if (!isOwner && !(isAdmin || isModerator))
        {
            Log.Warning("User {userId} is not allowed to delete comment {commentId}", userId, id);
            return new Response<string>(HttpStatusCode.Forbidden, "Forbidden");
        }

        existingComment.IsDeleted = true;
        var result = await context.SaveChangesAsync();

        if (result == 0)
        {
            return new Response<string>(HttpStatusCode.BadRequest, "Failed to delete the comment");
        }

        return new Response<string>(HttpStatusCode.OK, "Deleted the comment successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unexpected error in DeleteCommentAsync");
        return new Response<string>(HttpStatusCode.InternalServerError, "Failed to delete the comment");
    }
}

}