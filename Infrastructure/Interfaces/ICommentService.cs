using Domain.DTOs.Comment;
using Infrastructure.Responses;

namespace Infrastructure.Interfaces;

public interface ICommentService
{
    Task<Response<string>> CreateCommentAsync(CreateCommentDto dto);
    Task<Response<List<GetCommentDto>>> GetAllCommentAsync(Guid? videoId, Guid? newsId);
    Task<Response<string>> UpdateCommentAsync(UpdateCommentDto dto);
    Task<Response<string>> DeleteCommentAsync(Guid id);
}