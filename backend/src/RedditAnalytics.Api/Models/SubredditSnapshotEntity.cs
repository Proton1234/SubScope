/*
 * SubredditSnapshotEntity.cs
 *
 * Stores point-in-time subreddit counts captured after successful profile refreshes.
 */
namespace RedditAnalytics.Api.Models;

public class SubredditSnapshotEntity
{
    public int Id { get; set; }
    public int SubredditId { get; set; }
    public int SubscriberCount { get; set; }
    public int? ActiveAccountCount { get; set; }
    public DateTime CapturedAtUtc { get; set; }

    public SubredditEntity Subreddit { get; set; } = null!;
}
