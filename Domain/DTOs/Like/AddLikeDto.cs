using Domain.Enums;

namespace Domain.DTOs.Like;

public class AddLikeDto
{
    public Guid TargetId { get; set; }
    public LikeType Type { get; set; }
}