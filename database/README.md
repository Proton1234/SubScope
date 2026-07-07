# Database

PostgreSQL stores saved subreddit metadata for SubScope.

## Current Schema

`init.sql` creates the current MVP table:

- `Subreddits`

The backend also calls EF Core `EnsureCreated()` during startup. The SQL initialization script is kept aligned with the current entity shape so a fresh Docker volume starts with the expected table.

## Future Work

Historical post snapshots, migrations, seed data, and analytics views are intentionally left for a later milestone.
