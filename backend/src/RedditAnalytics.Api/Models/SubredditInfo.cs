/*
 * SubredditInfo.cs
 *
 * Carries a normalized Reddit profile from RedditService to SubredditController.
 * It separates external Reddit JSON from the database entity and public API response.
 */
namespace RedditAnalytics.Api.Models;

// Internal profile model used between the Reddit client and the API controller.
public class SubredditInfo
{
    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int SubscriberCount { get; set; }
    public int? ActiveAccountCount { get; set; }
    public DateTime CreatedUtc { get; set; }
}
