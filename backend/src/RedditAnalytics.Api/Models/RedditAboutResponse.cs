using System.Text.Json.Serialization;

namespace RedditAnalytics.Api.Models;

public class RedditAboutResponse
{
    [JsonPropertyName("data")]
    public RedditAboutData? Data { get; set; }
}

public class RedditAboutData
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("public_description")]
    public string? PublicDescription { get; set; }

    [JsonPropertyName("subscribers")]
    public int Subscribers { get; set; }

    [JsonPropertyName("active_account_count")]
    public int? ActiveAccounts { get; set; }

    [JsonPropertyName("created_utc")]
    public double CreatedUtc { get; set; }
}
