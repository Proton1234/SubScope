/*
 * SubredditAnalytics.cs
 *
 * Defines the calculated engagement data returned by the analytics endpoint.
 * SubredditController builds it from the hot-post summaries supplied by RedditService.
 */
namespace RedditAnalytics.Api.Models;

public class SubredditAnalytics
{
    public string SubredditName { get; set; } = null!;
    public int PostsAnalyzed { get; set; }
    public double AverageScore { get; set; }
    public double AverageComments { get; set; }
    public double EngagementPerSubscriber { get; set; }
    public RedditPostSummary? TopPostByScore { get; set; }
    public RedditPostSummary? TopPostByComments { get; set; }
    public DateTime FetchedUtc { get; set; }
}

public class RedditPostSummary
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int Score { get; set; }
    public int CommentCount { get; set; }
    public string Url { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
}
