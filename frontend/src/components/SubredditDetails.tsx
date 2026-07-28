/*
 * SubredditDetails.tsx
 *
 * Renders the profile returned after a successful subreddit search.
 * App supplies the selected profile and controls when this panel appears.
 */
import { SubredditResponse } from '../types/api';

interface SubredditDetailsProps {
  subreddit: SubredditResponse;
  heading: string;
}

export function SubredditDetails({ subreddit, heading }: SubredditDetailsProps) {
  return (
    <section className="panel detail-panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">{heading}</p>
          <h2>{subreddit.title}</h2>
        </div>
        <span className="count-pill">r/{subreddit.name}</span>
      </div>
      <p className="description">{subreddit.description || 'No description provided.'}</p>
      <div className="metric-grid">
        <div className="metric-card">
          <span>Subscribers</span>
          <strong>{subreddit.subscriberCount.toLocaleString()}</strong>
        </div>
        <div className="metric-card">
          <span>Created</span>
          <strong>{new Date(subreddit.createdUtc).toLocaleDateString()}</strong>
        </div>
      </div>
    </section>
  );
}
