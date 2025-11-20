using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Infrastructure.BackgroundTasks;

public class CalculateNewsLikesTask(IServiceScopeFactory serviceScopeFactory)
{
    public async Task CalculateNewsLikes()
    {
        Log.Information("Started background task: \"Calculate News Likes\". {dateTime}", DateTime.UtcNow);
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"NewsStats\"");

        Log.Information("Deleted old news stats");
        
        var news = await dbContext.News.Include(n => n.Likes).Where(n => !n.IsDeleted).ToListAsync();
        
        var newsStats = news.Select(n => new NewsStats
        {
            Id = Guid.NewGuid(),
            NewsId = n.Id,
            TotalLikes = n.Likes.Count(l => l.Type == LikeType.Like),
            TotalDislikes = n.Likes.Count(l => l.Type == LikeType.Dislike),
            LastCalculatedAt = DateTime.UtcNow
        }).ToList();
        
        
        await dbContext.NewsStats.AddRangeAsync(newsStats);
        await dbContext.SaveChangesAsync();
        
        Log.Information("Calculated likes for {count} news", newsStats.Count);
    }
}