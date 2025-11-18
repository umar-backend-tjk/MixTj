namespace Domain.DTOs.Comment;

public class GetCommentDto
{
    public Guid Id { get; set; }
    public required string Text { get; set; }
    public string? Reply { get; set; }
    public Guid UserId { get; set; }
    public Guid? NewsId { get; set; }
    public Guid? VideoId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}