using Domain.DTOs.Like;
using Domain.DTOs.News;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController(INewsService newsService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<Response<string>> CreateNewsAsync(CreateNewsDto dto)
        => await newsService.CreateNewsAsync(dto);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpPut]
    public async Task<Response<string>> UpdateNewsAsync(UpdateNewsDto dto)
        => await newsService.UpdateNewsAsync(dto);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpDelete]
    public async Task<Response<string>> DeleteNewsAsync(Guid id)
        => await newsService.DeleteNewsAsync(id);
    
    [HttpGet("{id}")]
    public async Task<Response<GetNewsDto>> GetNewsByIdAsync(Guid id)
        => await newsService.GetNewsByIdAsync(id);
    
    [HttpGet]
    public async Task<PaginationResponse<List<GetNewsDto>>> GetAllNewsAsync([FromQuery] NewsFilter filter)
        => await newsService.GetAllNewsAsync(filter);

    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpPost("likes/add-like")]
    public async Task<Response<string>> AddLikeAsync([FromBody] AddLikeDto likeDto)
    {
        return await newsService.AddLikeAsync(likeDto);
    }

    [AllowAnonymous]
    [HttpGet("{id}/likes")]
    public async Task<Response<List<GetLikeDto>>> GetAllLikesAsync(Guid id)
        => await newsService.GetAllLikesAsync(id);
    
    [Authorize(Roles = "Admin, Moderator, User")]
    [HttpDelete("{id}/likes")]
    public async Task<Response<string>> RemoveLikeAsync(Guid id)
        => await newsService.RemoveLikeAsync(id);
}