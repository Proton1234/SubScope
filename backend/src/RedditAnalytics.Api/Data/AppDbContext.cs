using Microsoft.EntityFrameworkCore;
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SubredditEntity> Subreddits => Set<SubredditEntity>();
}
