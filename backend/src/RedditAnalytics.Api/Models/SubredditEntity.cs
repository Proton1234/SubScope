/*
 * SubredditEntity.cs
 *
 * Defines the subreddit profile persisted in PostgreSQL through AppDbContext.
 * SubredditController creates or refreshes these records after a successful search.
 */
namespace RedditAnalytics.Api.Models;

// Persisted profile data lets the dashboard load saved communities without calling Reddit.
public class SubredditEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int SubscriberCount { get; set; }
    public int? ActiveAccountCount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<SubredditSnapshotEntity> Snapshots { get; set; } = new();
}
