namespace RedditAnalytics.Api.Models;

public sealed class RedditSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string AccessTokenUrl { get; set; } = "https://www.reddit.com/api/v1/access_token";
    public string ApiBaseUrl { get; set; } = "https://oauth.reddit.com";
}
