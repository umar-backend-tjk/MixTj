using Domain.Enums;

namespace Domain.DTOs.News;

public class UpdateNewsDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public Category Category { get; set; }
    public string[]? Tags { get; set; }
}