using Domain.Enums;

namespace Domain.Entities;

public class News : BaseEntity
{
    public required string AuthorId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public Category Category { get; set; }
    public string[]? Tags { get; set; } 
    
    public List<Comment> Comments { get; set; } = new List<Comment>();
    public List<Like> Likes { get; set; } = new List<Like>();
}