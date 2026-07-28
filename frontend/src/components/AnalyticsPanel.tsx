/*
 * AnalyticsPanel.tsx
 *
 * Presents engagement metrics calculated by the backend from live Reddit posts.
 * App supplies the analytics data and its loading or error state.
 */
import { RedditPostSummary, SubredditAnalyticsResponse } from '../types/api';

interface AnalyticsPanelProps {
  analytics: SubredditAnalyticsResponse | null;
  loading: boolean;
  error: string | null;
}

function formatPercent(value: number): string {
  return `${(value * 100).toFixed(2)}%`;
}

function TopPost({ label, post }: { label: string; post?: RedditPostSummary }) {
  if (!post) {
    return null;
  }

  return (
    <article className="top-post-card">
      <span>{label}</span>
      <h3>{post.title}</h3>
      <p>
          {post.score.toLocaleString()} score - {post.commentCount.toLocaleString()} comments
      </p>
      {post.url && (
        <a className="post-link" href={post.url} target="_blank" rel="noreferrer">
          View post
        </a>
      )}
    </article>
  );
}

export function AnalyticsPanel({ analytics, loading, error }: AnalyticsPanelProps) {
  return (
    <section className="panel analytics-panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Recent activity</p>
          <h2>Post engagement</h2>
        </div>
      </div>

      {loading && <p className="muted">Loading recent post analytics...</p>}
      {error && <p className="error-message">{error}</p>}
      {!analytics && !loading && !error && (
        <div className="empty-insights">
          <strong>No community selected yet</strong>
          <p>Search a community or choose a saved one to see engagement, top posts, and discussion activity.</p>
        </div>
      )}
      {analytics && !loading && (
        <div>
          <p className="panel-subtitle">
            Analyzed {analytics.postsAnalyzed} hot posts from r/{analytics.subredditName} at{' '}
            {new Date(analytics.fetchedUtc).toLocaleString()}.
          </p>
          <div className="metric-grid analytics-metrics">
            <div className="metric-card">
              <span>Posts analyzed</span>
              <strong>{analytics.postsAnalyzed}</strong>
            </div>
            <div className="metric-card">
              <span>Average score</span>
              <strong>{analytics.averageScore.toLocaleString()}</strong>
            </div>
            <div className="metric-card">
              <span>Average comments</span>
              <strong>{analytics.averageComments.toLocaleString()}</strong>
            </div>
            <div className="metric-card">
              <span>Engagement per subscriber</span>
              <strong>{formatPercent(analytics.engagementPerSubscriber)}</strong>
            </div>
          </div>
          <div className="top-post-grid">
            <TopPost label="Top post by score" post={analytics.topPostByScore} />
            <TopPost label="Top post by comments" post={analytics.topPostByComments} />
          </div>
        </div>
      )}
    </section>
  );
}
