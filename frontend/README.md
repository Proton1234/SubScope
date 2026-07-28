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

In Docker, nginx serves the built app and proxies `/api` requests to the backend service. Set `API_PROXY_PASS` to the backend origin used by nginx, such as `http://backend:80` in Docker Compose or the internal backend Container App URL in Azure. Set `API_PROXY_HOST` to the backend host expected by the proxy target, such as `backend` locally or the backend Container App internal FQDN in Azure.
