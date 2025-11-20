using Domain.DTOs.Like;
using Domain.DTOs.Video;
using Domain.Filters;
using Infrastructure.Responses;

namespace Infrastructure.Interfaces;

public interface IVideoService
{
    Task<Response<string>> CreateVideoAsync(CreateVideoDto createDto);
    Task<PaginationResponse<List<GetVideoDto>>> GetAllVideoAsync(VideoFilter filter);
    Task<Response<GetVideoDto>> GetVideoByIdAsync(Guid videoId);
    Task<Response<string>> UpdateVideoAsync(UpdateVideoDto updateDto);
    Task<Response<string>> DeleteVideoAsync(Guid videoId);
    Task<Response<string>> AddLikeAsync(AddLikeDto dto);
    Task<Response<List<GetLikeDto>>> GetAllLikes(Guid targetId);
    Task<Response<string>> RemoveLike(Guid targetId);
}