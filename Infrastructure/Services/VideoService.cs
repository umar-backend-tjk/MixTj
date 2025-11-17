using Domain.DTOs.Video;
using Domain.Filters;
using Infrastructure.Interfaces;
using Infrastructure.Responses;

namespace Infrastructure.Services;

public class VideoService : IVideoService
{
    public Task<Response<string>> CreateVideoAsync(CreateVideoDto createDto)
    {
        throw new NotImplementedException();
    }

    public Task<PaginationResponse<List<GetVideoDto>>> GetAllVideoAsync(VideoFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<Response<GetVideoDto>> GetVideoByIdAsync(Guid videoId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> UpdateVideoAsync(UpdateVideoDto updateDto)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> DeleteVideoAsync(Guid videoId)
    {
        throw new NotImplementedException();
    }
}