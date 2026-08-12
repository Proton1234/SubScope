/*
 * ISubredditAnalyticsService.cs
 *
 * Defines cached subreddit engagement analytics calculated from Reddit hot posts.
 */
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

public interface ISubredditAnalyticsService
{
    Task<SubredditAnalytics?> GetAnalyticsAsync(SubredditEntity subreddit, CancellationToken cancellationToken = default);
}
