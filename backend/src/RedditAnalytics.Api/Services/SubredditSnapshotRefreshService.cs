/*
 * SubredditSnapshotRefreshService.cs
 *
 * Periodically refreshes saved subreddit profiles and records historical snapshots.
 */
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RedditAnalytics.Api.Data;
using RedditAnalytics.Api.Models;

namespace RedditAnalytics.Api.Services;

public class SubredditSnapshotRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubredditSnapshotRefreshService> _logger;
    private readonly SnapshotRefreshSettings _settings;

    public SubredditSnapshotRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubredditSnapshotRefreshService> logger,
        IOptions<SnapshotRefreshSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = GetRefreshInterval();
        _logger.LogInformation("Subreddit snapshot refresh service starting with interval {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected failure during subreddit snapshot refresh cycle.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Subreddit snapshot refresh service stopped.");
    }

    private TimeSpan GetRefreshInterval()
    {
        return _settings.IntervalMinutes > 0
            ? TimeSpan.FromMinutes(_settings.IntervalMinutes)
            : TimeSpan.FromMinutes(15);
    }

    public async Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var redditService = scope.ServiceProvider.GetRequiredService<IRedditService>();

        var subreddits = await db.Subreddits
            .AsNoTracking()
            .OrderBy(subreddit => subreddit.Name)
            .Select(subreddit => new SavedSubreddit(subreddit.Id, subreddit.Name))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Starting subreddit snapshot refresh cycle for {SubredditCount} saved subreddits.", subreddits.Count);

        var refreshedCount = 0;
        foreach (var savedSubreddit in subreddits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var freshData = await redditService.GetSubredditAsync(savedSubreddit.Name, cancellationToken);
                if (freshData is null)
                {
                    _logger.LogWarning("Skipping snapshot refresh for r/{SubredditName}; Reddit returned no profile data.", savedSubreddit.Name);
                    continue;
                }

                var entity = await db.Subreddits.SingleOrDefaultAsync(
                    subreddit => subreddit.Id == savedSubreddit.Id,
                    cancellationToken);
                if (entity is null)
                {
                    _logger.LogWarning("Skipping snapshot refresh for r/{SubredditName}; saved subreddit no longer exists.", savedSubreddit.Name);
                    continue;
                }

                entity.Title = freshData.Title;
                entity.Description = freshData.Description;
                entity.SubscriberCount = freshData.SubscriberCount;
                entity.ActiveAccountCount = freshData.ActiveAccountCount;
                entity.CreatedUtc = freshData.CreatedUtc;

                db.SubredditSnapshots.Add(new SubredditSnapshotEntity
                {
                    SubredditId = entity.Id,
                    SubscriberCount = entity.SubscriberCount,
                    ActiveAccountCount = entity.ActiveAccountCount,
                    CapturedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(cancellationToken);
                refreshedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                db.ChangeTracker.Clear();
                _logger.LogError(ex, "Failed to refresh snapshot for r/{SubredditName}.", savedSubreddit.Name);
            }
        }

        _logger.LogInformation(
            "Completed subreddit snapshot refresh cycle. Refreshed {RefreshedCount} of {SubredditCount} saved subreddits.",
            refreshedCount,
            subreddits.Count);
    }

    private record SavedSubreddit(int Id, string Name);
}
