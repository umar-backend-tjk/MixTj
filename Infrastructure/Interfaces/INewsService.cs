using Domain.DTOs.News;
using Domain.Filters;
using Infrastructure.Responses;

namespace Infrastructure.Interfaces;

public interface INewsService
{
    Task<Response<string>> CreateNewsAsync(CreateNewsDto dto);
    Task<PaginationResponse<List<GetNewsDto>>> GetAllNewsAsync(NewsFilter filter);
    Task<Response<GetNewsDto>> GetNewsByIdAsync(Guid id);
    Task<Response<string>> UpdateNewsAsync(UpdateNewsDto dto);
    Task<Response<string>> DeleteNewsAsync(Guid id);
}