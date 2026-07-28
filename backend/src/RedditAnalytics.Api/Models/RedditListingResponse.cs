/*
 * RedditListingResponse.cs
 *
 * Maps Reddit's nested hot-post listing response.
 * RedditService filters and converts these records into public post summaries.
 */
using System.Text.Json.Serialization;

namespace RedditAnalytics.Api.Models;

// Reddit listings wrap each post in a child object under data.children.
public class RedditListingResponse
{
    [JsonPropertyName("data")]
    public RedditListingData? Data { get; set; }
}

public class RedditListingData
{
    [JsonPropertyName("children")]
    public List<RedditPostChild> Children { get; set; } = new();
}

public class RedditPostChild
{
    [JsonPropertyName("data")]
    public RedditPostData? Data { get; set; }
}

public class RedditPostData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("num_comments")]
    public int CommentCount { get; set; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; set; }

    [JsonPropertyName("created_utc")]
    public double CreatedUtc { get; set; }
}
