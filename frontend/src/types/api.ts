export interface ApiErrorResponse {
  error?: string;
}

export interface SubredditRequest {
  subredditName: string;
}

export interface SubredditResponse {
  id: number;
  name: string;
  title: string;
  description?: string;
  subscriberCount: number;
  activeAccountCount: number | null;
  createdUtc: string;
}

export interface RedditPostSummary {
  id: string;
  title: string;
  score: number;
  commentCount: number;
  url: string;
  createdUtc: string;
}

export interface SubredditAnalyticsResponse {
  subredditName: string;
  postsAnalyzed: number;
  averageScore: number;
  averageComments: number;
  engagementPerSubscriber: number;
  topPostByScore?: RedditPostSummary;
  topPostByComments?: RedditPostSummary;
  fetchedUtc: string;
}
