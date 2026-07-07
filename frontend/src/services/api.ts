import { ApiErrorResponse, SubredditAnalyticsResponse, SubredditResponse } from '../types/api';

const apiBase = (import.meta.env.VITE_API_BASE_URL as string) || '/api';

function normalizeSubredditName(subredditName: string): string {
  return subredditName.trim().replace(/^r\//i, '');
}

async function throwApiError(response: Response, fallbackMessage: string): Promise<never> {
  let message = fallbackMessage;

  try {
    const body = (await response.json()) as ApiErrorResponse;
    if (body.error) {
      message = body.error;
    }
  } catch {
    // Keep the fallback when the response body is not JSON.
  }

  throw new Error(message);
}

export async function fetchSubreddit(subredditName: string): Promise<SubredditResponse> {
  const response = await fetch(`${apiBase}/subreddit`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ subredditName })
  });

  if (!response.ok) {
    await throwApiError(response, 'Unable to fetch subreddit.');
  }

  return response.json();
}

export async function fetchSavedSubreddit(subredditName: string): Promise<SubredditResponse> {
  const normalizedName = normalizeSubredditName(subredditName);
  const response = await fetch(`${apiBase}/subreddit/${encodeURIComponent(normalizedName)}`);

  if (!response.ok) {
    await throwApiError(response, 'Unable to load saved subreddit.');
  }

  return response.json();
}

export async function fetchSavedSubreddits(): Promise<SubredditResponse[]> {
  const response = await fetch(`${apiBase}/subreddit`);

  if (!response.ok) {
    await throwApiError(response, 'Unable to load saved subreddits.');
  }

  return response.json();
}

export async function fetchSubredditAnalytics(subredditName: string): Promise<SubredditAnalyticsResponse> {
  const normalizedName = normalizeSubredditName(subredditName);
  const response = await fetch(`${apiBase}/subreddit/${encodeURIComponent(normalizedName)}/analytics`);

  if (!response.ok) {
    await throwApiError(response, 'Unable to load subreddit analytics.');
  }

  return response.json();
}
