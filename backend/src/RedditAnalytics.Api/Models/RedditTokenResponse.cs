/*
 * RedditTokenResponse.cs
 *
 * Maps the OAuth token response returned by Reddit.
 * RedditService caches the access token and uses its lifetime to decide when to refresh it.
 */
using System.Text.Json.Serialization;

namespace RedditAnalytics.Api.Models;

public class RedditTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = null!;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}
