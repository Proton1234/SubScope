/*
 * Program.cs
 *
 * Configures the API's services, database, Reddit client, CORS policy, and routes.
 * It also prepares the database at startup and exposes the container health endpoint.
 */
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using RedditAnalytics.Api.Data;
using RedditAnalytics.Api.Models;
using RedditAnalytics.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RedditSettings>(builder.Configuration.GetSection("RedditSettings"));
builder.Services.Configure<SnapshotRefreshSettings>(builder.Configuration.GetSection("SnapshotRefresh"));

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient("RedditOAuth", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SubScope/1.0");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<IRedditTokenProvider, RedditTokenProvider>();
builder.Services.AddHostedService<SubredditSnapshotRefreshService>();
builder.Services.AddScoped<ISubredditAnalyticsService, SubredditAnalyticsService>();

builder.Services.AddHttpClient<IRedditService, RedditService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default") ??
        builder.Configuration["ConnectionStrings:Default"]));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// The production frontend shares the API origin through nginx; only local Vite needs CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal",
        policy => policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var retries = 10;
        var delay = TimeSpan.FromSeconds(3);

        // Docker may start the API before PostgreSQL is ready to accept connections.
        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                db.Database.EnsureCreated();
                break;
            }
            catch (Exception ex) when (attempt < retries)
            {
                Console.WriteLine($"Attempt {attempt} failed to connect to database: {ex.Message}");
                await Task.Delay(delay);
            }
        }
    }
}

app.UseCors("AllowLocal");

app.MapControllers();

// Include database connectivity so container health checks catch more than a running process.
app.MapGet("/api/health", async (AppDbContext db) =>
{
    var databaseConnected = await db.Database.CanConnectAsync();
    var response = new
    {
        status = databaseConnected ? "Healthy" : "Unhealthy",
        database = databaseConnected ? "Connected" : "Unavailable",
        checkedUtc = DateTime.UtcNow
    };

    return databaseConnected ? Results.Ok(response) : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();
