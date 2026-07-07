# Frontend

React + TypeScript dashboard for SubScope.

## Structure

- `src/components/` - reusable dashboard components
- `src/services/` - API client functions
- `src/styles/` - shared CSS
- `src/types/` - shared TypeScript interfaces

## Commands

```powershell
npm install
npm run build
```

In Docker, nginx serves the built app and proxies `/api` requests to the backend service.
