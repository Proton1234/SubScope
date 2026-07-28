/*
 * SnapshotRefreshSettings.cs
 *
 * Configures the background snapshot refresh interval.
 */
namespace RedditAnalytics.Api.Models;

public sealed class SnapshotRefreshSettings
{
    public double IntervalMinutes { get; set; } = 15;
}
