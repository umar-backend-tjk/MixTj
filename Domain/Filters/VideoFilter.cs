namespace Domain.Filters;

public class VideoFilter : BaseFilter
{
    public string? AuthorId { get; set; }
    public string? Title { get; set; }
}