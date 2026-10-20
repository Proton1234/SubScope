using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RedditAnalytics.Api.Controllers;
using RedditAnalytics.Api.Data;
using RedditAnalytics.Api.Models;
using RedditAnalytics.Api.Services;
using Xunit;

namespace RedditAnalytics.Api.Tests;

public class SubredditHistoryTests
{
    [Fact]
    public async Task Post_CreatesExactlyOneSubredditSnapshot()
    {
        using var database = new SqliteTestDatabase();
        var redditService = new Mock<IRedditService>();
        redditService
            .Setup(service => service.GetSubredditAsync("dotnet", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubredditInfo
            {
                Name = "dotnet",
                Title = ".NET",
                Description = ".NET community",
                SubscriberCount = 100,
                ActiveAccountCount = null,
                CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
            });

        await using var db = database.CreateContext();
        var controller = new SubredditController(redditService.Object, Mock.Of<ISubredditAnalyticsService>(), db);

        var result = await controller.Post(new SubredditController.SubredditRequest("r/dotnet"));

        Assert.IsType<OkObjectResult>(result);
        var snapshot = Assert.Single(await db.SubredditSnapshots.ToListAsync());
        Assert.Equal(100, snapshot.SubscriberCount);
        Assert.Null(snapshot.ActiveAccountCount);
    }

    [Fact]
    public async Task GetHistory_ReturnsSavedSnapshotsInChronologicalOrder()
    {
        using var database = new SqliteTestDatabase();
        await using (var db = database.CreateContext())
        {
            var subreddit = new SubredditEntity
            {
                Name = "dotnet",
                Title = ".NET",
                SubscriberCount = 100,
                CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
            };

            db.Subreddits.Add(subreddit);
            await db.SaveChangesAsync();
            db.SubredditSnapshots.AddRange(
                new SubredditSnapshotEntity
                {
                    SubredditId = subreddit.Id,
                    SubscriberCount = 300,
                    CapturedAtUtc = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubredditSnapshotEntity
                {
                    SubredditId = subreddit.Id,
                    SubscriberCount = 200,
                    CapturedAtUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
                });
            await db.SaveChangesAsync();
        }

        await using var assertionDb = database.CreateContext();
        var controller = new SubredditController(
            Mock.Of<IRedditService>(),
            Mock.Of<ISubredditAnalyticsService>(),
            assertionDb);

        var result = await controller.GetHistory("r/dotnet");

        var ok = Assert.IsType<OkObjectResult>(result);
        var snapshots = Assert.IsAssignableFrom<IEnumerable<SubredditController.SubredditSnapshotResponse>>(ok.Value).ToList();
        Assert.Collection(
            snapshots,
            first => Assert.Equal(200, first.SubscriberCount),
            second => Assert.Equal(300, second.SubscriberCount));
    }

    [Fact]
    public async Task GetHistory_ReturnsEmptyResultForSavedSubredditWithNoSnapshots()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        db.Subreddits.Add(new SubredditEntity
        {
            Name = "dotnet",
            Title = ".NET",
            SubscriberCount = 100,
            CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();
        var controller = new SubredditController(Mock.Of<IRedditService>(), Mock.Of<ISubredditAnalyticsService>(), db);

        var result = await controller.GetHistory("dotnet");

        var ok = Assert.IsType<OkObjectResult>(result);
        var snapshots = Assert.IsAssignableFrom<IEnumerable<SubredditController.SubredditSnapshotResponse>>(ok.Value);
        Assert.Empty(snapshots);
    }

    [Fact]
    public async Task GetHistory_WithLimitDownsamplesSnapshotsAndKeepsFirstAndLast()
    {
        using var database = new SqliteTestDatabase();
        await using (var db = database.CreateContext())
        {
            var subreddit = new SubredditEntity
            {
                Name = "dotnet",
                Title = ".NET",
                SubscriberCount = 100,
                CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
            };

            db.Subreddits.Add(subreddit);
            await db.SaveChangesAsync();

            for (var index = 0; index < 10; index++)
            {
                db.SubredditSnapshots.Add(new SubredditSnapshotEntity
                {
                    SubredditId = subreddit.Id,
                    SubscriberCount = 100 + index,
                    CapturedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index)
                });
            }

            await db.SaveChangesAsync();
        }

        await using var assertionDb = database.CreateContext();
        var controller = new SubredditController(
            Mock.Of<IRedditService>(),
            Mock.Of<ISubredditAnalyticsService>(),
            assertionDb);

        var result = await controller.GetHistory("dotnet", limit: 4);

        var ok = Assert.IsType<OkObjectResult>(result);
        var snapshots = Assert.IsAssignableFrom<IEnumerable<SubredditController.SubredditSnapshotResponse>>(ok.Value).ToList();
        Assert.Collection(
            snapshots,
            first => Assert.Equal(100, first.SubscriberCount),
            second => Assert.Equal(103, second.SubscriberCount),
            third => Assert.Equal(106, third.SubscriberCount),
            fourth => Assert.Equal(109, fourth.SubscriberCount));
    }

    [Fact]
    public async Task AnalyticsService_CacheHitDoesNotCallRedditAgain()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var subreddit = CreateSubreddit("dotnet", subscriberCount: 100);
        var redditService = new Mock<IRedditService>();
        redditService
            .Setup(service => service.GetHotPostsAsync("dotnet", 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePosts());
        var analyticsService = new SubredditAnalyticsService(redditService.Object, cache);

        var first = await analyticsService.GetAnalyticsAsync(subreddit);
        var second = await analyticsService.GetAnalyticsAsync(subreddit);

        Assert.NotNull(first);
        Assert.Same(first, second);
        redditService.Verify(
            service => service.GetHotPostsAsync("dotnet", 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyticsService_ConcurrentRequestsForSameSubredditShareRedditCall()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var subreddit = CreateSubreddit("dotnet", subscriberCount: 100);
        var redditCallCount = 0;
        var redditService = new Mock<IRedditService>();
        redditService
            .Setup(service => service.GetHotPostsAsync("dotnet", 25, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref redditCallCount);
                await Task.Delay(50);
                return CreatePosts();
            });
        var analyticsService = new SubredditAnalyticsService(redditService.Object, cache);

        var requests = Enumerable.Range(0, 10)
            .Select(_ => analyticsService.GetAnalyticsAsync(subreddit))
            .ToArray();
        var results = await Task.WhenAll(requests);

        Assert.All(results, Assert.NotNull);
        Assert.Equal(1, redditCallCount);
        redditService.Verify(
            service => service.GetHotPostsAsync("dotnet", 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BackgroundRefresh_UpdatesSavedSubredditAndInsertsOneSnapshot()
    {
        using var database = new SqliteTestDatabase();
        await using (var db = database.CreateContext())
        {
            db.Subreddits.Add(new SubredditEntity
            {
                Name = "dotnet",
                Title = "Old title",
                Description = "Old description",
                SubscriberCount = 100,
                ActiveAccountCount = null,
                CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
            });
            await db.SaveChangesAsync();
        }

        var redditService = new Mock<IRedditService>();
        redditService
            .Setup(service => service.GetSubredditAsync("dotnet", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubredditInfo
            {
                Name = "dotnet",
                Title = "Fresh title",
                Description = "Fresh description",
                SubscriberCount = 250,
                ActiveAccountCount = 12,
                CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
            });

        await using var provider = database.CreateServiceProvider(redditService.Object);
        var refreshService = CreateRefreshService(provider);

        await refreshService.RefreshOnceAsync();

        await using var assertionDb = database.CreateContext();
        var subreddit = await assertionDb.Subreddits.SingleAsync(subreddit => subreddit.Name == "dotnet");
        var snapshot = Assert.Single(await assertionDb.SubredditSnapshots.ToListAsync());
        Assert.Equal("Fresh title", subreddit.Title);
        Assert.Equal("Fresh description", subreddit.Description);
        Assert.Equal(250, subreddit.SubscriberCount);
        Assert.Equal(12, subreddit.ActiveAccountCount);
        Assert.Equal(250, snapshot.SubscriberCount);
        Assert.Equal(12, snapshot.ActiveAccountCount);
    }

    [Fact]
    public async Task BackgroundRefresh_SubredditFailureDoesNotPreventRemainingSubredditsFromProcessing()
    {
        using var database = new SqliteTestDatabase();
        await using (var db = database.CreateContext())
        {
            db.Subreddits.AddRange(
                new SubredditEntity
                {
                    Name = "alpha",
                    Title = "Alpha",
                    SubscriberCount = 100,
                    CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubredditEntity
                {
                    Name = "beta",
                    Title = "Beta",
                    SubscriberCount = 200,
                    CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
                });
            await db.SaveChangesAsync();
        }

        var redditService = new Mock<IRedditService>();
        redditService
            .Setup(service => service.GetSubredditAsync("alpha", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Synthetic failure"));
        redditService
            .Setup(service => service.GetSubredditAsync("beta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubredditInfo
            {
                Name = "beta",
                Title = "Beta refreshed",
                SubscriberCount = 300,
                ActiveAccountCount = null,
                CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
            });

        await using var provider = database.CreateServiceProvider(redditService.Object);
        var refreshService = CreateRefreshService(provider);

        await refreshService.RefreshOnceAsync();

        await using var assertionDb = database.CreateContext();
        var alpha = await assertionDb.Subreddits.SingleAsync(subreddit => subreddit.Name == "alpha");
        var beta = await assertionDb.Subreddits.SingleAsync(subreddit => subreddit.Name == "beta");
        var snapshot = Assert.Single(await assertionDb.SubredditSnapshots.ToListAsync());
        Assert.Equal(100, alpha.SubscriberCount);
        Assert.Equal(300, beta.SubscriberCount);
        Assert.Equal(beta.Id, snapshot.SubredditId);
        Assert.Equal(300, snapshot.SubscriberCount);
    }

    private static SubredditSnapshotRefreshService CreateRefreshService(IServiceProvider serviceProvider)
    {
        return new SubredditSnapshotRefreshService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SubredditSnapshotRefreshService>.Instance,
            Options.Create(new SnapshotRefreshSettings()));
    }

    private static SubredditEntity CreateSubreddit(string name, int subscriberCount)
    {
        return new SubredditEntity
        {
            Name = name,
            Title = name,
            SubscriberCount = subscriberCount,
            CreatedUtc = new DateTime(2008, 1, 25, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static List<RedditPostSummary> CreatePosts()
    {
        return new List<RedditPostSummary>
        {
            new()
            {
                Id = "post-1",
                Title = "Post 1",
                Score = 10,
                CommentCount = 2,
                Url = "https://www.reddit.com/r/dotnet/comments/post-1",
                CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = "post-2",
                Title = "Post 2",
                Score = 20,
                CommentCount = 4,
                Url = "https://www.reddit.com/r/dotnet/comments/post-2",
                CreatedUtc = new DateTime(2026, 1, 1, 0, 15, 0, DateTimeKind.Utc)
            }
        };
    }

    private sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public SqliteTestDatabase()
        {
            _connection.Open();
            using var db = CreateContext();
            db.Database.EnsureCreated();
        }

        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            return new AppDbContext(options);
        }

        public ServiceProvider CreateServiceProvider(IRedditService redditService)
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            services.AddSingleton(redditService);
            return services.BuildServiceProvider();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
