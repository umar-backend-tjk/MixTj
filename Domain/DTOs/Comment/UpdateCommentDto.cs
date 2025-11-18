using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Comment;

public class UpdateCommentDto
{
    [Required]
    public Guid Id { get; set; }
    [MaxLength(1000)]
    public required string Text { get; set; }
    public string? Reply { get; set; }
    public Guid? NewsId { get; set; }
    public Guid? VideoId { get; set; }
}