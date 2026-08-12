/*
 * SubredditAnalyticsService.cs
 *
 * Computes and caches subreddit engagement analytics from Reddit hot posts.
 */
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

public class SubredditAnalyticsService : ISubredditAnalyticsService
{
    private const int HotPostCount = 25;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new();

    private readonly IRedditService _redditService;
    private readonly IMemoryCache _cache;

    public SubredditAnalyticsService(IRedditService redditService, IMemoryCache cache)
    {
        _redditService = redditService;
        _cache = cache;
    }

    public async Task<SubredditAnalytics?> GetAnalyticsAsync(SubredditEntity subreddit, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(subreddit.Name);
        if (_cache.TryGetValue(cacheKey, out SubredditAnalytics? cachedAnalytics))
        {
            return cachedAnalytics;
        }

        var cacheLock = CacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(cancellationToken);

        try
        {
            if (_cache.TryGetValue(cacheKey, out cachedAnalytics))
            {
                return cachedAnalytics;
            }

            var analytics = await BuildAnalyticsAsync(subreddit, cancellationToken);
            if (analytics is null)
            {
                return null;
            }

            _cache.Set(cacheKey, analytics, CacheDuration);
            return analytics;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private async Task<SubredditAnalytics?> BuildAnalyticsAsync(SubredditEntity subreddit, CancellationToken cancellationToken)
    {
        var posts = await _redditService.GetHotPostsAsync(subreddit.Name, HotPostCount, cancellationToken);
        if (posts is null)
        {
            return null;
        }

        var postsAnalyzed = posts.Count;
        var averageScore = postsAnalyzed == 0 ? 0 : posts.Average(post => post.Score);
        var averageComments = postsAnalyzed == 0 ? 0 : posts.Average(post => post.CommentCount);
        // This is a lightweight comparison metric, not Reddit's internal engagement formula.
        var totalEngagement = posts.Sum(post => post.Score + post.CommentCount);
        var engagementPerSubscriber = subreddit.SubscriberCount == 0 ? 0 : (double)totalEngagement / subreddit.SubscriberCount;

        return new SubredditAnalytics
        {
            SubredditName = subreddit.Name,
            PostsAnalyzed = postsAnalyzed,
            AverageScore = Math.Round(averageScore, 2),
            AverageComments = Math.Round(averageComments, 2),
            EngagementPerSubscriber = Math.Round(engagementPerSubscriber, 6),
            TopPostByScore = posts.OrderByDescending(post => post.Score).FirstOrDefault(),
            TopPostByComments = posts.OrderByDescending(post => post.CommentCount).FirstOrDefault(),
            FetchedUtc = DateTime.UtcNow
        };
    }

    private static string GetCacheKey(string subredditName)
    {
        return $"subreddit-analytics:{subredditName.ToLowerInvariant()}";
    }
}
