using Domain.DTOs.Comment;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<string>> CreateCommentAsync(CreateCommentDto dto)
        => await commentService.CreateCommentAsync(dto);
    
    [HttpPut]
    public async Task<Response<string>> UpdateCommentAsync(UpdateCommentDto dto)
        => await commentService.UpdateCommentAsync(dto);
    
    [HttpDelete]
    public async Task<Response<string>> DeleteCommentAsync(Guid id)
        => await commentService.DeleteCommentAsync(id);
    
    [HttpGet]
    public async Task<Response<List<GetCommentDto>>> GetAllCommentsAsync(Guid? videoId, Guid? newsId)
        => await commentService.GetAllCommentAsync(videoId, newsId);
}