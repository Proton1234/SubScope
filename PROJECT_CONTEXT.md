# Project Context – SubScope

Read this before making changes to the project.

## Overview

**SubScope** is a Reddit community analytics dashboard (portfolio project).

- **Public name:** SubScope
- **Internal namespaces:** RedditAnalytics (unchanged to avoid refactoring)
- **Status:** Version 1 feature complete
- **Not affiliated with Reddit**; uses Reddit's OAuth API

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18, TypeScript, Vite |
| Backend | ASP.NET Core (.NET 8), C# |
| Database | PostgreSQL, EF Core |
| Infrastructure | Docker, Docker Compose, Nginx |
| CI | GitHub Actions |

## Version 1 Status – All Milestones Complete

| Milestone | Features |
|-----------|----------|
| 1 | Reddit OAuth, subreddit search, metadata persistence, PostgreSQL |
| 2 | Saved communities retrieval, dashboard, community loading |
| 3 | Live `/hot` endpoint, analytics (avg score, comments, engagement, top post, activity) |
| 4 | Docker stability, CI cleanup, README, health endpoint, security |
| 5 | Release prep, branding, documentation, final cleanup |

**The application works.** All core features are functional.

## Security

- `.env` ignored and not tracked
- `appsettings.Development.json` removed from tracking
- README disclaimer added
- Secret scan completed
- Public release branch: `release/subscope-v1` (single clean commit)

## Current Goal – GitHub Readiness

**Focus on:**
- UI polish and visual refinement
- UX improvements and interactions
- Quality-of-life enhancements
- README and documentation polish
- GitHub publication readiness

**Avoid:**
- Authentication / user accounts
- JWT / sessions
- New analytics features
- Major architecture changes

These belong in Version 2.

## Design Philosophy

The UI should feel like a professional analytics dashboard.

**Priorities:**
- Current community is the primary focus
- Analytics dashboard is the centerpiece
- Saved communities are secondary
- Reduce unnecessary whitespace
- Improve spacing and visual hierarchy
- Keep the design original (don't copy RedditInsight, etc.)

## Before Making Changes

1. Explain what you plan to change
2. Keep changes focused
3. Verify the application builds
4. Preserve existing functionality

## Application State

**Frontend:**
- Search subreddit
- Saved communities list
- Community overview
- Analytics dashboard

**Backend:**
- Reddit OAuth
- Metadata endpoint
- Analytics endpoint
- Health endpoint

**Infrastructure:**
- Docker builds successfully
- Docker Compose configured
