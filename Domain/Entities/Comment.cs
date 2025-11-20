namespace Domain.Entities;

public class Comment : BaseEntity
{
    public required string Text { get; set; }
    public string? Reply { get; set; }
    public required string UserId { get; set; }
    public Guid? NewsId { get; set; }
    public Guid? VideoId { get; set; }
    
    public List<Like> Likes = [];
}