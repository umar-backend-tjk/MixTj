using Domain.DTOs.Comment;
using Domain.DTOs.Like;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<Response<string>> CreateCommentAsync(CreateCommentDto dto)
        => await commentService.CreateCommentAsync(dto);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpPut]
    public async Task<Response<string>> UpdateCommentAsync(UpdateCommentDto dto)
        => await commentService.UpdateCommentAsync(dto);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpDelete]
    public async Task<Response<string>> DeleteCommentAsync(Guid id)
        => await commentService.DeleteCommentAsync(id);
    
    [HttpGet]
    public async Task<Response<List<GetCommentDto>>> GetAllCommentsAsync(Guid? videoId, Guid? newsId)
        => await commentService.GetAllCommentAsync(videoId, newsId);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpPost("likes/add-like")]
    public async Task<Response<string>> AddLikeAsync([FromBody] AddLikeDto likeDto)
    {
        return await commentService.AddLikeAsync(likeDto);
    }

    [AllowAnonymous]
    [HttpGet("{id}/likes")]
    public async Task<Response<List<GetLikeDto>>> GetAllLikesAsync(Guid id)
        => await commentService.GetAllLikesAsync(id);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpDelete("{id}/likes")]
    public async Task<Response<string>> RemoveLikeAsync(Guid id)
        => await commentService.RemoveLikeAsync(id);
}