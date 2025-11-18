using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Comment;

public class CreateCommentDto : IValidatableObject
{
    [Required]
    public required string Text { get; set; }
    public Guid? NewsId { get; set; }
    public Guid? VideoId { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((NewsId == null && VideoId == null) || (NewsId != null && VideoId != null))
        {
            yield return new ValidationResult(
                "Exactly one of NewsId or VideoId must be provided.",
                new[] { nameof(NewsId), nameof(VideoId) }
            );
        }
    }
}