/*
 * SubredditController.cs
 *
 * Handles profile and analytics requests from the React frontend.
 * It validates names, coordinates RedditService and AppDbContext, and returns API response models.
 */
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedditAnalytics.Api.Data;
using RedditAnalytics.Api.Models;
using RedditAnalytics.Api.Services;

namespace RedditAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Subreddit profiles are persisted, while post analytics are calculated from live Reddit data.
public class SubredditController : ControllerBase
{
    private readonly IRedditService _redditService;
    private readonly ISubredditAnalyticsService _analyticsService;
    private readonly AppDbContext _db;

    public SubredditController(IRedditService redditService, ISubredditAnalyticsService analyticsService, AppDbContext db)
    {
        _redditService = redditService;
        _analyticsService = analyticsService;
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

        // Searching doubles as an upsert so saved profile counts stay reasonably current.
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

        var snapshot = new SubredditSnapshotEntity
        {
            SubredditId = entity.Id,
            SubscriberCount = entity.SubscriberCount,
            ActiveAccountCount = entity.ActiveAccountCount,
            CapturedAtUtc = DateTime.UtcNow
        };

        _db.SubredditSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(entity));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Put the largest saved communities first for a more useful default dashboard.
        var subreddits = await _db.Subreddits
            .AsNoTracking()
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

        var entity = await _db.Subreddits
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == normalizedName);
        return entity is null ? NotFound(new { error = "Subreddit not found in database." }) : Ok(ToResponse(entity));
    }

    [HttpGet("{name}/history")]
    public async Task<IActionResult> GetHistory(string name, [FromQuery] int? limit = null)
    {
        var normalizedName = NormalizeSubredditName(name);
        if (!IsValidSubredditName(normalizedName))
        {
            return BadRequest(new { error = "Enter a valid subreddit name using 2-21 letters, numbers, or underscores." });
        }

        if (limit is <= 1)
        {
            return BadRequest(new { error = "History limit must be at least 2." });
        }

        var entity = await _db.Subreddits
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == normalizedName);
        if (entity is null)
        {
            return NotFound(new { error = "Subreddit not found in database. Search Reddit first to persist it." });
        }

        var snapshots = await _db.SubredditSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.SubredditId == entity.Id)
            .OrderBy(snapshot => snapshot.CapturedAtUtc)
            .Select(snapshot => new SubredditSnapshotResponse(
                snapshot.SubscriberCount,
                snapshot.ActiveAccountCount,
                snapshot.CapturedAtUtc))
            .ToListAsync();

        return Ok(limit.HasValue ? DownsampleSnapshots(snapshots, limit.Value) : snapshots);
    }

    [HttpGet("{name}/analytics")]
    public async Task<IActionResult> GetAnalytics(string name)
    {
        var normalizedName = NormalizeSubredditName(name);
        if (!IsValidSubredditName(normalizedName))
        {
            return BadRequest(new { error = "Enter a valid subreddit name using 2-21 letters, numbers, or underscores." });
        }

        var entity = await _db.Subreddits
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == normalizedName);
        if (entity is null)
        {
            return NotFound(new { error = "Subreddit not found in database. Search Reddit first to persist it." });
        }

        var analytics = await _analyticsService.GetAnalyticsAsync(entity);
        if (analytics is null)
        {
            return NotFound(new { error = "Unable to fetch recent Reddit posts for analytics." });
        }

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

    public record SubredditSnapshotResponse(
        int SubscriberCount,
        int? ActiveAccountCount,
        DateTime CapturedAtUtc);

    private static SubredditResponse ToResponse(SubredditEntity entity)
    {
        return new SubredditResponse(
            entity.Id,
            entity.Name,
            entity.Title,
            entity.Description,
            entity.SubscriberCount,
            entity.ActiveAccountCount,
            entity.CreatedUtc);
    }

    private static string NormalizeSubredditName(string? name)
    {
        // Accept the common "r/name" and "/r/name" forms used in copied Reddit links.
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

    private static IReadOnlyList<SubredditSnapshotResponse> DownsampleSnapshots(
        IReadOnlyList<SubredditSnapshotResponse> snapshots,
        int limit)
    {
        if (snapshots.Count <= limit)
        {
            return snapshots;
        }

        var sampled = new List<SubredditSnapshotResponse>(capacity: limit);
        var lastIndex = snapshots.Count - 1;
        var previousIndex = -1;

        for (var sampleIndex = 0; sampleIndex < limit; sampleIndex++)
        {
            var sourceIndex = (int)Math.Round((double)sampleIndex * lastIndex / (limit - 1));
            if (sourceIndex == previousIndex)
            {
                continue;
            }

            sampled.Add(snapshots[sourceIndex]);
            previousIndex = sourceIndex;
        }

        return sampled;
    }
}
