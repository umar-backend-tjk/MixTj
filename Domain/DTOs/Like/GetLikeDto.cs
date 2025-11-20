using Domain.Enums;

namespace Domain.DTOs.Like;

public class GetLikeDto
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public Guid TargetId { get; set; }
    public LikeType Type { get; set; }
}