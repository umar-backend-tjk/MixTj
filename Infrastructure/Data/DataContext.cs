using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DataContext(DbContextOptions options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<News> News { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<NewsStats> NewsStats { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.TargetId })
            .IsUnique();
        
        base.OnModelCreating(builder);
    }
}