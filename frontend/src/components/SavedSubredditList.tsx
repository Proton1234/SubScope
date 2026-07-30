/*
 * SavedSubredditList.tsx
 *
 * Displays subreddit profiles already stored by the backend.
 * It lets App know when a user wants fresh analytics for a saved community.
 */
import { SubredditResponse } from '../types/api';

interface SavedSubredditListProps {
  subreddits: SubredditResponse[];
  loading: boolean;
  error: string | null;
  analyticsLoading: boolean;
  selectedSubredditName: string | null;
  onViewAnalytics: (subreddit: SubredditResponse) => void;
}

export function SavedSubredditList({
  subreddits,
  loading,
  error,
  analyticsLoading,
  selectedSubredditName,
  onViewAnalytics
}: SavedSubredditListProps) {
  return (
    <section className="panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Saved</p>
          <h2>Tracked communities</h2>
        </div>
        <span className="count-pill">{subreddits.length} saved</span>
      </div>

      {loading && <p className="muted">Loading saved communities...</p>}
      {error && <p className="error-message">{error}</p>}
      {!loading && !error && subreddits.length === 0 && (
        <p className="muted">Search for a community to start tracking it.</p>
      )}
      {!loading && !error && subreddits.length > 0 && (
        <div className="saved-grid">
          {subreddits.map((saved) => {
            const isSelected = saved.name === selectedSubredditName;

            return (
              <article key={saved.id} className={`subreddit-card ${isSelected ? 'selected-subreddit-card' : ''}`}>
                <div className="card-header">
                  <div>
                    <h3>{saved.title}</h3>
                    <p>r/{saved.name}</p>
                  </div>
                  <span>{new Date(saved.createdUtc).toLocaleDateString()}</span>
                </div>
                <p className="description compact-description">{saved.description || 'No description available.'}</p>
                <div className="mini-metrics single-metric">
                  <div>
                    <strong>{saved.subscriberCount.toLocaleString()}</strong>
                    <span>Subscribers</span>
                  </div>
                </div>
                <button className="text-button" onClick={() => onViewAnalytics(saved)} disabled={analyticsLoading}>
                  Inspect insights
                </button>
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}
