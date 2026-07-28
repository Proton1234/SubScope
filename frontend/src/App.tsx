/*
 * App.tsx
 *
 * Root component that owns shared dashboard state and coordinates data loading.
 * It connects the API client to the search, saved-community, detail, and analytics panels.
 */
import { useEffect, useState } from 'react';
import { AnalyticsPanel } from './components/AnalyticsPanel';
import { HistoryPanel } from './components/HistoryPanel';
import { PrivacyPage } from './components/PrivacyPage';
import { SavedSubredditList } from './components/SavedSubredditList';
import { SearchPanel } from './components/SearchPanel';
import { SubredditDetails } from './components/SubredditDetails';
import {
  fetchSavedSubreddits,
  fetchSubreddit,
  fetchSubredditAnalytics,
  fetchSubredditHistory
} from './services/api';
import { SubredditAnalyticsResponse, SubredditHistorySnapshot, SubredditResponse } from './types/api';

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}

function App() {
  if (window.location.pathname === '/privacy') {
    return <PrivacyPage />;
  }

  // Profile, saved-list, and analytics requests report independently so one failure does not hide the rest.
  const [subredditName, setSubredditName] = useState('programming');
  const [subreddit, setSubreddit] = useState<SubredditResponse | null>(null);
  const [savedSubreddits, setSavedSubreddits] = useState<SubredditResponse[]>([]);
  const [analytics, setAnalytics] = useState<SubredditAnalyticsResponse | null>(null);
  const [history, setHistory] = useState<SubredditHistorySnapshot[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [savedError, setSavedError] = useState<string | null>(null);
  const [analyticsError, setAnalyticsError] = useState<string | null>(null);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [savedLoading, setSavedLoading] = useState(false);
  const [analyticsLoading, setAnalyticsLoading] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);

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

  const loadHistory = async (name: string) => {
    setHistoryLoading(true);
    setHistoryError(null);

    try {
      const data = await fetchSubredditHistory(name);
      setHistory(data);
    } catch (err) {
      setHistoryError(getErrorMessage(err, 'Failed to load subscriber history.'));
    } finally {
      setHistoryLoading(false);
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
    setHistory([]);

    try {
      const data = await fetchSubreddit(subredditName);
      setSubreddit(data);

      // A successful search updates the database, so refresh saved data before loading live analytics.
      await loadSavedSubreddits();
      await loadHistory(data.name);
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
          {subreddit && <HistoryPanel history={history} loading={historyLoading} error={historyError} />}
        </div>
      </div>

      <footer className="site-footer">
        <span>SubScope is an independent portfolio project.</span>
        <a href="/privacy">Privacy</a>
      </footer>
    </main>
  );
}

export default App;
