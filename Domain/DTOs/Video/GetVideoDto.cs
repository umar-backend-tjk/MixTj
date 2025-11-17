namespace Domain.DTOs.Video;

public class GetVideoDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string VideoPath { get; set; }
    public required string AuthorId  { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}