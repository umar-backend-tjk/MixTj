using Domain.DTOs.Like;
using Domain.DTOs.News;
using Domain.Entities;
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
    Task<Response<string>> AddLikeAsync(AddLikeDto dto);
    Task<Response<List<GetLikeDto>>> GetAllLikesAsync(Guid targetId);
    Task<Response<string>> RemoveLikeAsync(Guid targetId);
}