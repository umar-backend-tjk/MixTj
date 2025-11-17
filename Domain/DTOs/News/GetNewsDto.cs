using Domain.Enums;

namespace Domain.DTOs.News;

public class GetNewsDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public Category Category { get; set; }
    public string[]? Tags { get; set; }
    public required string AuthorId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}