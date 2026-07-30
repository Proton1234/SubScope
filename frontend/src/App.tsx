/*
 * App.tsx
 *
 * Root component that owns shared dashboard state and coordinates data loading.
 * It connects the API client to the search, saved-community, detail, and analytics panels.
 */
import { useCallback, useEffect, useRef, useState } from 'react';
import { AnalyticsPanel } from './components/AnalyticsPanel';
import { HistoryPanel } from './components/HistoryPanel';
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
  // Profile, saved-list, and analytics requests report independently so one failure does not hide the rest.
  const [subredditName, setSubredditName] = useState('');
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
  const [hasSelectedCommunity, setHasSelectedCommunity] = useState(false);
  const latestSelectionId = useRef(0);

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

  const loadAnalytics = useCallback(async (name: string, selectionId: number) => {
    setAnalyticsLoading(true);
    setAnalyticsError(null);

    try {
      const data = await fetchSubredditAnalytics(name);
      if (latestSelectionId.current === selectionId) {
        setAnalytics(data);
      }
    } catch (err) {
      if (latestSelectionId.current === selectionId) {
        setAnalyticsError(getErrorMessage(err, 'Failed to load recent post analytics.'));
      }
    } finally {
      if (latestSelectionId.current === selectionId) {
        setAnalyticsLoading(false);
      }
    }
  }, []);

  const loadHistory = useCallback(async (name: string, selectionId: number) => {
    setHistoryLoading(true);
    setHistoryError(null);

    try {
      const data = await fetchSubredditHistory(name);
      if (latestSelectionId.current === selectionId) {
        setHistory(data);
      }
    } catch (err) {
      if (latestSelectionId.current === selectionId) {
        setHistoryError(getErrorMessage(err, 'Failed to load subscriber history.'));
      }
    } finally {
      if (latestSelectionId.current === selectionId) {
        setHistoryLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    loadSavedSubreddits();
  }, []);

  const handleSearch = async () => {
    const selectionId = latestSelectionId.current + 1;
    latestSelectionId.current = selectionId;
    setLoading(true);
    setError(null);
    setSubreddit(null);
    setAnalytics(null);
    setHistory([]);
    setHasSelectedCommunity(true);

    try {
      const data = await fetchSubreddit(subredditName);
      if (latestSelectionId.current !== selectionId) {
        return;
      }

      setSubreddit(data);

      // A successful search updates the database, so refresh saved data before loading live analytics.
      await loadSavedSubreddits();
      await loadHistory(data.name, selectionId);
      await loadAnalytics(data.name, selectionId);
    } catch (err) {
      if (latestSelectionId.current === selectionId) {
        setError(getErrorMessage(err, 'Failed to fetch subreddit. Make sure the name is valid.'));
      }
    } finally {
      if (latestSelectionId.current === selectionId) {
        setLoading(false);
      }
    }
  };

  const handleInspectSavedSubreddit = useCallback(async (saved: SubredditResponse, updateSearchInput = true) => {
    const selectionId = latestSelectionId.current + 1;
    latestSelectionId.current = selectionId;
    setLoading(false);
    setError(null);
    setSubreddit(saved);
    if (updateSearchInput) {
      setSubredditName(saved.name);
    }
    setAnalytics(null);
    setHistory([]);
    setHasSelectedCommunity(true);

    await loadHistory(saved.name, selectionId);
    await loadAnalytics(saved.name, selectionId);
  }, [loadAnalytics, loadHistory]);

  useEffect(() => {
    if (!hasSelectedCommunity && !subreddit && savedSubreddits.length > 0) {
      void handleInspectSavedSubreddit(savedSubreddits[0], false);
    }
  }, [handleInspectSavedSubreddit, hasSelectedCommunity, savedSubreddits, subreddit]);

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
          selectedSubredditName={subreddit?.name ?? null}
          onViewAnalytics={handleInspectSavedSubreddit}
        />

        <div className="insights-stack">
          {subreddit && <SubredditDetails subreddit={subreddit} heading="Current community" />}
          <AnalyticsPanel analytics={analytics} loading={analyticsLoading} error={analyticsError} />
          {subreddit && <HistoryPanel history={history} loading={historyLoading} error={historyError} />}
        </div>
      </div>
    </main>
  );
}

export default App;
