# Backend

ASP.NET Core Web API backend for SubScope.

## Structure

- `src/RedditAnalytics.Api/Controllers/` - API controllers
- `src/RedditAnalytics.Api/Models/` - persistence, Reddit response, and API response models
- `src/RedditAnalytics.Api/Services/` - Reddit OAuth/API integration
- `src/RedditAnalytics.Api/Data/` - EF Core DbContext

## Responsibilities

- Fetch subreddit metadata from Reddit.
- Persist saved subreddit records in PostgreSQL.
- Periodically refresh saved subreddit records and store historical snapshots.
- Fetch live `/hot` posts and compute analytics.
- Expose API/database health through `/api/health`.
