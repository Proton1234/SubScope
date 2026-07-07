import { useEffect, useState } from 'react';
import { AnalyticsPanel } from './components/AnalyticsPanel';
import { SavedSubredditList } from './components/SavedSubredditList';
import { SearchPanel } from './components/SearchPanel';
import { SubredditDetails } from './components/SubredditDetails';
import {
  fetchSavedSubreddits,
  fetchSubreddit,
  fetchSubredditAnalytics
} from './services/api';
import { SubredditAnalyticsResponse, SubredditResponse } from './types/api';

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}

function App() {
  const [subredditName, setSubredditName] = useState('programming');
  const [subreddit, setSubreddit] = useState<SubredditResponse | null>(null);
  const [savedSubreddits, setSavedSubreddits] = useState<SubredditResponse[]>([]);
  const [analytics, setAnalytics] = useState<SubredditAnalyticsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [savedError, setSavedError] = useState<string | null>(null);
  const [analyticsError, setAnalyticsError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [savedLoading, setSavedLoading] = useState(false);
  const [analyticsLoading, setAnalyticsLoading] = useState(false);

  const loadSavedSubreddits = async () => {
    setSavedLoading(true);
    setSavedError(null);

    try {
      const saved = await fetchSavedSubreddits();
      setSavedSubreddits(saved);
    } catch (err) {
      setSavedError(getErrorMessage(err, 'Failed to load saved subreddit data from the database.'));
    } finally {
      setSavedLoading(false);
    }
  };

  const loadAnalytics = async (name: string) => {
    setAnalyticsLoading(true);
    setAnalyticsError(null);

    try {
      const data = await fetchSubredditAnalytics(name);
      setAnalytics(data);
    } catch (err) {
      setAnalyticsError(getErrorMessage(err, 'Failed to load recent post analytics.'));
    } finally {
      setAnalyticsLoading(false);
    }
  };

  useEffect(() => {
    loadSavedSubreddits();
  }, []);

  const handleSearch = async () => {
    setLoading(true);
    setError(null);
    setSubreddit(null);
    setAnalytics(null);

    try {
      const data = await fetchSubreddit(subredditName);
      setSubreddit(data);
      await loadSavedSubreddits();
      await loadAnalytics(data.name);
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to fetch subreddit. Make sure the name is valid.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="app-shell">
      <SearchPanel
        subredditName={subredditName}
        loading={loading}
        onSubredditNameChange={setSubredditName}
        onSearch={handleSearch}
      />

      {error && <p className="error-message top-error">{error}</p>}

      <div className="dashboard-grid">
        <SavedSubredditList
          subreddits={savedSubreddits}
          loading={savedLoading}
          error={savedError}
          analyticsLoading={analyticsLoading}
          onViewAnalytics={loadAnalytics}
        />

        <div className="insights-stack">
          {subreddit && <SubredditDetails subreddit={subreddit} heading="Current community" />}
          <AnalyticsPanel analytics={analytics} loading={analyticsLoading} error={analyticsError} />
        </div>
      </div>
    </main>
  );
}

export default App;
