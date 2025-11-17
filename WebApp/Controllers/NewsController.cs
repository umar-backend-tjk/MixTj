using Domain.DTOs.News;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Responses;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController(INewsService newsService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<string>> CreateNewsAsync(CreateNewsDto dto)
        => await newsService.CreateNewsAsync(dto);
    
    [HttpPut]
    public async Task<Response<string>> UpdateNewsAsync(UpdateNewsDto dto)
        => await newsService.UpdateNewsAsync(dto);
    
    [HttpDelete]
    public async Task<Response<string>> DeleteNewsAsync(Guid id)
        => await newsService.DeleteNewsAsync(id);
    
    [HttpGet("{id}")]
    public async Task<Response<GetNewsDto>> GetNewsByIdAsync(Guid id)
        => await newsService.GetNewsByIdAsync(id);
    
    [HttpGet]
    public async Task<PaginationResponse<List<GetNewsDto>>> GetAllNewsAsync([FromQuery] NewsFilter filter)
        => await newsService.GetAllNewsAsync(filter);
}