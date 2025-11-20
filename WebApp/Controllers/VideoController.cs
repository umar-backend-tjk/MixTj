using Domain.DTOs.Like;
using Domain.DTOs.Video;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController(IVideoService videoService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<Response<string>> CreateVideoAsync(CreateVideoDto dto)
        => await videoService.CreateVideoAsync(dto);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpPut]
    public async Task<Response<string>> UpdateVideoAsync(UpdateVideoDto dto)
        => await videoService.UpdateVideoAsync(dto);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpDelete]
    public async Task<Response<string>> DeleteVideoAsync(Guid id)
        => await videoService.DeleteVideoAsync(id);
    
    [AllowAnonymous]
    [HttpGet]
    public async Task<PaginationResponse<List<GetVideoDto>>> GetAllVideosAsync([FromQuery] VideoFilter filter)
        => await videoService.GetAllVideoAsync(filter);
    
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<Response<GetVideoDto>> GetVideoByIdAsync(Guid id)
        => await videoService.GetVideoByIdAsync(id);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpPost("likes/add-like")]
    public async Task<Response<string>> AddLikeAsync([FromBody] AddLikeDto likeDto)
    {
        return await videoService.AddLikeAsync(likeDto);
    }

    [AllowAnonymous]
    [HttpGet("{id}/likes")]
    public async Task<Response<List<GetLikeDto>>> GetAllLikesAsync(Guid id)
        => await videoService.GetAllLikesAsync(id);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpDelete("{id}/likes")]
    public async Task<Response<string>> RemoveLikeAsync(Guid id)
        => await videoService.RemoveLikeAsync(id);
}