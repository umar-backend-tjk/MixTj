using Domain.DTOs.Video;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController(IVideoService videoService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<string>> CreateVideoAsync(CreateVideoDto dto)
        => await videoService.CreateVideoAsync(dto);
    
    [HttpPut]
    public async Task<Response<string>> UpdateVideoAsync(UpdateVideoDto dto)
        => await videoService.UpdateVideoAsync(dto);
    
    [HttpDelete]
    public async Task<Response<string>> DeleteVideoAsync(Guid id)
        => await videoService.DeleteVideoAsync(id);
    
    [HttpGet]
    public async Task<PaginationResponse<List<GetVideoDto>>> GetAllVideosAsync([FromQuery] VideoFilter filter)
        => await videoService.GetAllVideoAsync(filter);
    
    [HttpGet("{id}")]
    public async Task<Response<GetVideoDto>> GetVideoByIdAsync(Guid id)
        => await videoService.GetVideoByIdAsync(id);
}