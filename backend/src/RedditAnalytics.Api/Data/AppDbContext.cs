/*
 * AppDbContext.cs
 *
 * Defines the Entity Framework session used to read and save subreddit profiles.
 * The controller receives it through dependency injection for each API request.
 */
using Microsoft.EntityFrameworkCore;
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SubredditEntity> Subreddits => Set<SubredditEntity>();
    public DbSet<SubredditSnapshotEntity> SubredditSnapshots => Set<SubredditSnapshotEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubredditEntity>()
            .HasIndex(subreddit => subreddit.Name)
            .IsUnique();

        modelBuilder.Entity<SubredditSnapshotEntity>()
            .HasOne(snapshot => snapshot.Subreddit)
            .WithMany(subreddit => subreddit.Snapshots)
            .HasForeignKey(snapshot => snapshot.SubredditId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubredditSnapshotEntity>()
            .HasIndex(snapshot => new { snapshot.SubredditId, snapshot.CapturedAtUtc });
    }
}
