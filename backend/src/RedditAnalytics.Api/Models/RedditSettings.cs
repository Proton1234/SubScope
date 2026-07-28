/*
 * RedditSettings.cs
 *
 * Holds Reddit OAuth credentials and endpoint settings loaded from application configuration.
 * RedditService receives these values through the options system.
 */
namespace RedditAnalytics.Api.Models;

public sealed class RedditSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string AccessTokenUrl { get; set; } = "https://www.reddit.com/api/v1/access_token";
    public string ApiBaseUrl { get; set; } = "https://oauth.reddit.com";
}
