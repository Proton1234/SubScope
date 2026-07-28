# SubScope API Reference

Base URL in Docker:

- Through frontend nginx proxy: `http://localhost:3000/api`
- Direct backend port: `http://localhost:5000/api`

## Health

### `GET /api/health`

Returns API and database connectivity status.

Example response:

```json
{
  "status": "Healthy",
  "database": "Connected",
  "checkedUtc": "2026-07-07T18:33:09.671543Z"
}
```

## Subreddits

### `POST /api/subreddit`

Fetches live subreddit metadata from Reddit, then inserts or updates the saved database record.

Request body:

```json
{
  "subredditName": "r/dotnet"
}
```

Example response:

```json
{
  "id": 1,
  "name": "dotnet",
  "title": ".NET",
  "description": ".NET Community...",
  "subscriberCount": 240233,
  "activeAccountCount": null,
  "createdUtc": "2008-01-25T06:47:32Z"
}
```

Validation:

- Accepts `dotnet`, `r/dotnet`, or `/r/dotnet`.
- Subreddit names must use 2-21 letters, numbers, or underscores.

### `GET /api/subreddit`

Returns all saved subreddit records, ordered by subscriber count descending.

### `GET /api/subreddit/{name}`

Returns one saved subreddit record from PostgreSQL.

Example:

```text
GET /api/subreddit/dotnet
```

### `GET /api/subreddit/{name}/history`

Returns historical snapshots for a saved subreddit, ordered oldest to newest. This endpoint reads PostgreSQL and does not contact Reddit.

Example response:

```json
[
  {
    "subscriberCount": 240233,
    "activeAccountCount": null,
    "capturedAtUtc": "2026-07-28T18:33:11.4384521Z"
  }
]
```

### `GET /api/subreddit/{name}/analytics`

Fetches recent hot posts from Reddit and computes live engagement analytics. Posts are not persisted.

Reddit source endpoint:

```text
GET https://oauth.reddit.com/r/{subreddit}/hot?limit=25
```

Example response:

```json
{
  "subredditName": "dotnet",
  "postsAnalyzed": 25,
  "averageScore": 24.12,
  "averageComments": 24.92,
  "engagementPerSubscriber": 0.005103,
  "topPostByScore": {
    "id": "1unl8x0",
    "title": "I hate Kendo Ui MVC",
    "score": 251,
    "commentCount": 47,
    "url": "https://www.reddit.com/r/dotnet/comments/...",
    "createdUtc": "2026-07-04T22:00:23Z"
  },
  "topPostByComments": {
    "id": "1unlkuv",
    "title": "Announcing LibreWPF",
    "score": 103,
    "commentCount": 87,
    "url": "https://www.reddit.com/r/dotnet/comments/...",
    "createdUtc": "2026-07-04T22:15:24Z"
  },
  "fetchedUtc": "2026-07-07T18:33:11.4384521Z"
}
```

## Error Shape

Most API errors return:

```json
{
  "error": "Human-readable message."
}
```
