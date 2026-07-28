/*
 * RedditService.cs
 *
 * Retrieves subreddit profiles and hot posts from Reddit.
 * OAuth token caching is delegated to RedditTokenProvider.
 */
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

public class RedditService : IRedditService
{
    private readonly HttpClient _httpClient;
    private readonly IRedditTokenProvider _tokenProvider;
    private readonly RedditSettings _settings;

    public RedditService(HttpClient httpClient, IRedditTokenProvider tokenProvider, IOptions<RedditSettings> settings)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _settings = settings.Value;
    }

    public async Task<SubredditInfo?> GetSubredditAsync(string subredditName, CancellationToken cancellationToken = default)
    {
        var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        // Reddit's /about endpoint supplies the profile fields stored in our database.
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ApiBaseUrl}/r/{subredditName}/about");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("SubScope/1.0");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<RedditAboutResponse>(cancellationToken);
        var data = result?.Data;
        if (data is null)
        {
            return null;
        }

        return new SubredditInfo
        {
            Name = data.DisplayName ?? subredditName,
            Title = data.Title ?? string.Empty,
            Description = data.PublicDescription,
            SubscriberCount = data.Subscribers,
            ActiveAccountCount = data.ActiveAccounts,
            CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)data.CreatedUtc).UtcDateTime
        };
    }

    public async Task<IReadOnlyList<RedditPostSummary>?> GetHotPostsAsync(string subredditName, int limit, CancellationToken cancellationToken = default)
    {
        var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        // Reddit caps listing requests at 100 items.
        var safeLimit = Math.Clamp(limit, 1, 100);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ApiBaseUrl}/r/{subredditName}/hot?limit={safeLimit}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("SubScope/1.0");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<RedditListingResponse>(cancellationToken);
        var posts = result?.Data?.Children
            .Select(child => child.Data)
            .Where(data => data is not null && !string.IsNullOrWhiteSpace(data.Id) && !string.IsNullOrWhiteSpace(data.Title))
            .Select(data => new RedditPostSummary
            {
                Id = data!.Id!,
                Title = data.Title!,
                Score = data.Score,
                CommentCount = data.CommentCount,
                Url = string.IsNullOrWhiteSpace(data.Permalink) ? string.Empty : $"https://www.reddit.com{data.Permalink}",
                CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)data.CreatedUtc).UtcDateTime
            })
            .ToList();

        return posts ?? new List<RedditPostSummary>();
    }
}
