using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

public interface IRedditService
{
    Task<SubredditInfo?> GetSubredditAsync(string subredditName);
    Task<IReadOnlyList<RedditPostSummary>?> GetHotPostsAsync(string subredditName, int limit);
}
