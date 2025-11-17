using Domain.Enums;

namespace Domain.Filters;

public class NewsFilter : BaseFilter
{
    public string? AuthorId { get; set; }
    public string? Title { get; set; }
    public Category? Category { get; set; }
    public string[]? Tags { get; set; }
}