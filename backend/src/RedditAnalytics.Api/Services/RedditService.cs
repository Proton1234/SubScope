using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

public class RedditService : IRedditService
{
    private readonly HttpClient _httpClient;
    private readonly RedditSettings _settings;
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public RedditService(HttpClient httpClient, IOptions<RedditSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<SubredditInfo?> GetSubredditAsync(string subredditName)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
        {
            return null;
        }

        if (!await EnsureAccessTokenAsync())
        {
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ApiBaseUrl}/r/{subredditName}/about");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.UserAgent.ParseAdd("SubScope/1.0");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<RedditAboutResponse>();
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
            ActiveAccountCount = data.ActiveAccounts ?? 0,
            CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)data.CreatedUtc).UtcDateTime
        };
    }

    public async Task<IReadOnlyList<RedditPostSummary>?> GetHotPostsAsync(string subredditName, int limit)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
        {
            return null;
        }

        if (!await EnsureAccessTokenAsync())
        {
            return null;
        }

        var safeLimit = Math.Clamp(limit, 1, 100);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ApiBaseUrl}/r/{subredditName}/hot?limit={safeLimit}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.UserAgent.ParseAdd("SubScope/1.0");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<RedditListingResponse>();
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

    private async Task<bool> EnsureAccessTokenAsync()
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
        {
            return true;
        }

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _settings.AccessTokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            })
        };

        var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        tokenRequest.Headers.Accept.ParseAdd("application/json");
        tokenRequest.Headers.UserAgent.ParseAdd("SubScope/1.0");

        var tokenResponse = await _httpClient.SendAsync(tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return false;
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<RedditTokenResponse>();
        if (tokenResult is null || string.IsNullOrWhiteSpace(tokenResult.AccessToken))
        {
            return false;
        }

        _accessToken = tokenResult.AccessToken;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResult.ExpiresIn - 30);
        return true;
    }
}
