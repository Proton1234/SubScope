/*
 * IRedditTokenProvider.cs
 *
 * Supplies cached Reddit OAuth access tokens to RedditService.
 * The implementation owns token refresh and concurrency control.
 */
namespace RedditAnalytics.Api.Services;

public interface IRedditTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
