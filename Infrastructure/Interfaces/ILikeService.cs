using Domain.DTOs.Like;
using Domain.DTOs.News;
using Domain.Entities;
using Domain.Filters;
using Infrastructure.Responses;

namespace Infrastructure.Interfaces;

public interface ILikeService
{
    Task<Response<string>> AddLikeAsync(AddLikeDto dto);
    Task<Response<List<GetLikeDto>>> GetAllLikes(Guid targetId);
    Task<Response<string>> RemoveLike(Guid targetId);
}