namespace Domain.Entities;

public class Video : BaseEntity
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string VideoPath { get; set; }
    public required string AuthorId  { get; set; }
}