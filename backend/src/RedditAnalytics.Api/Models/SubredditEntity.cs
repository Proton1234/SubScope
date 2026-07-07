namespace RedditAnalytics.Api.Models;

public class SubredditEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int SubscriberCount { get; set; }
    public int ActiveAccountCount { get; set; }
    public DateTime CreatedUtc { get; set; }
}
