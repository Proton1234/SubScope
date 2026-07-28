/*
 * IRedditService.cs
 *
 * Defines the Reddit operations needed by SubredditController.
 * Keeping this boundary separate prevents the controller from depending on HTTP details.
 */
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

// Keeps Reddit-specific authentication and response handling out of the controller.
public interface IRedditService
{
    Task<SubredditInfo?> GetSubredditAsync(string subredditName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RedditPostSummary>?> GetHotPostsAsync(string subredditName, int limit, CancellationToken cancellationToken = default);
}
