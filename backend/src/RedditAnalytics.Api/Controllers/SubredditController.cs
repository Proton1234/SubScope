using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedditAnalytics.Api.Data;
using RedditAnalytics.Api.Models;
using RedditAnalytics.Api.Services;

namespace RedditAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubredditController : ControllerBase
{
    private readonly IRedditService _redditService;
    private readonly AppDbContext _db;

    public SubredditController(IRedditService redditService, AppDbContext db)
    {
        _redditService = redditService;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SubredditRequest request)
    {
        var normalizedName = NormalizeSubredditName(request?.SubredditName);
        if (!IsValidSubredditName(normalizedName))
        {
            return BadRequest(new { error = "Enter a valid subreddit name using 2-21 letters, numbers, or underscores." });
        }

        var subredditData = await _redditService.GetSubredditAsync(normalizedName);
        if (subredditData is null)
        {
            return NotFound(new { error = "Subreddit not found or Reddit API unavailable." });
        }

        var entity = await _db.Subreddits.SingleOrDefaultAsync(x => x.Name == subredditData.Name);
        if (entity is null)
        {
            entity = new SubredditEntity
            {
                Name = subredditData.Name,
                Title = subredditData.Title,
                Description = subredditData.Description,
                SubscriberCount = subredditData.SubscriberCount,
                ActiveAccountCount = subredditData.ActiveAccountCount,
                CreatedUtc = subredditData.CreatedUtc
            };

            _db.Subreddits.Add(entity);
        }
        else
        {
            entity.Title = subredditData.Title;
            entity.Description = subredditData.Description;
            entity.SubscriberCount = subredditData.SubscriberCount;
            entity.ActiveAccountCount = subredditData.ActiveAccountCount;
            entity.CreatedUtc = subredditData.CreatedUtc;
        }

        await _db.SaveChangesAsync();

        return Ok(ToResponse(entity));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subreddits = await _db.Subreddits
            .OrderByDescending(x => x.SubscriberCount)
            .ToListAsync();

        return Ok(subreddits.Select(ToResponse));
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var normalizedName = NormalizeSubredditName(name);
        if (!IsValidSubredditName(normalizedName))
        {
            return BadRequest(new { error = "Enter a valid subreddit name using 2-21 letters, numbers, or underscores." });
        }

        var entity = await _db.Subreddits.SingleOrDefaultAsync(x => x.Name == normalizedName);
        return entity is null ? NotFound(new { error = "Subreddit not found in database." }) : Ok(ToResponse(entity));
    }

    [HttpGet("{name}/analytics")]
    public async Task<IActionResult> GetAnalytics(string name)
    {
        var normalizedName = NormalizeSubredditName(name);
        if (!IsValidSubredditName(normalizedName))
        {
            return BadRequest(new { error = "Enter a valid subreddit name using 2-21 letters, numbers, or underscores." });
        }

        var entity = await _db.Subreddits.SingleOrDefaultAsync(x => x.Name == normalizedName);
        if (entity is null)
        {
            return NotFound(new { error = "Subreddit not found in database. Search Reddit first to persist it." });
        }

        const int postLimit = 25;
        var posts = await _redditService.GetHotPostsAsync(normalizedName, postLimit);
        if (posts is null)
        {
            return NotFound(new { error = "Unable to fetch recent Reddit posts for analytics." });
        }

        var postsAnalyzed = posts.Count;
        var averageScore = postsAnalyzed == 0 ? 0 : posts.Average(post => post.Score);
        var averageComments = postsAnalyzed == 0 ? 0 : posts.Average(post => post.CommentCount);
        var totalEngagement = posts.Sum(post => post.Score + post.CommentCount);
        var engagementPerSubscriber = entity.SubscriberCount == 0 ? 0 : (double)totalEngagement / entity.SubscriberCount;

        var analytics = new SubredditAnalytics
        {
            SubredditName = entity.Name,
            PostsAnalyzed = postsAnalyzed,
            AverageScore = Math.Round(averageScore, 2),
            AverageComments = Math.Round(averageComments, 2),
            EngagementPerSubscriber = Math.Round(engagementPerSubscriber, 6),
            TopPostByScore = posts.OrderByDescending(post => post.Score).FirstOrDefault(),
            TopPostByComments = posts.OrderByDescending(post => post.CommentCount).FirstOrDefault(),
            FetchedUtc = DateTime.UtcNow
        };

        return Ok(analytics);
    }

    public record SubredditRequest(string SubredditName);

    public record SubredditResponse(
        int Id,
        string Name,
        string Title,
        string? Description,
        int SubscriberCount,
        int? ActiveAccountCount,
        DateTime CreatedUtc);

    private static SubredditResponse ToResponse(SubredditEntity entity)
    {
        return new SubredditResponse(
            entity.Id,
            entity.Name,
            entity.Title,
            entity.Description,
            entity.SubscriberCount,
            entity.ActiveAccountCount > 0 ? entity.ActiveAccountCount : null,
            entity.CreatedUtc);
    }

    private static string NormalizeSubredditName(string? name)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        if (normalizedName.StartsWith("/r/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = normalizedName[3..];
        }
        else if (normalizedName.StartsWith("r/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = normalizedName[2..];
        }

        return normalizedName;
    }

    private static bool IsValidSubredditName(string name)
    {
        return name.Length is >= 2 and <= 21 && name.All(character => char.IsLetterOrDigit(character) || character == '_');
    }
}
