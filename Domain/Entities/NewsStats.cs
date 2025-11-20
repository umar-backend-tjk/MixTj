namespace Domain.Entities;

public class NewsStats
{
    public Guid Id { get; set; }
    public Guid NewsId { get; set; }
    public int TotalLikes { get; set; }
    public int TotalDislikes { get; set; }
    public DateTime LastCalculatedAt { get; set; }
}