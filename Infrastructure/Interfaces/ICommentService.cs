using Domain.DTOs.Comment;
using Domain.DTOs.Like;
using Infrastructure.Responses;

namespace Infrastructure.Interfaces;

public interface ICommentService
{
    Task<Response<string>> CreateCommentAsync(CreateCommentDto dto);
    Task<Response<List<GetCommentDto>>> GetAllCommentAsync(Guid? videoId, Guid? newsId);
    Task<Response<string>> UpdateCommentAsync(UpdateCommentDto dto);
    Task<Response<string>> DeleteCommentAsync(Guid id);
    Task<Response<string>> AddLikeAsync(AddLikeDto dto);
    Task<Response<List<GetLikeDto>>> GetAllLikesAsync(Guid targetId);
    Task<Response<string>> RemoveLikeAsync(Guid targetId);
}