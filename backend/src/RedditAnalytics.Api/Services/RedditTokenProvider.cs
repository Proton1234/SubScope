/*
 * RedditTokenProvider.cs
 *
 * Caches Reddit OAuth access tokens across backend requests.
 * A single semaphore prevents concurrent refresh attempts from stampeding Reddit.
 */
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

public class RedditTokenProvider : IRedditTokenProvider
{
    private const int ExpirationBufferSeconds = 30;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RedditSettings _settings;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public RedditTokenProvider(IHttpClientFactory httpClientFactory, IOptions<RedditSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasValidToken())
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (HasValidToken())
            {
                return _accessToken;
            }

            return await RefreshAccessTokenAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool HasValidToken()
    {
        return _accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt;
    }

    private async Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
        {
            return null;
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

        var httpClient = _httpClientFactory.CreateClient("RedditOAuth");
        var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<RedditTokenResponse>(cancellationToken);
        if (tokenResult is null || string.IsNullOrWhiteSpace(tokenResult.AccessToken))
        {
            return null;
        }

        _accessToken = tokenResult.AccessToken;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResult.ExpiresIn - ExpirationBufferSeconds);

        return _accessToken;
    }
}
