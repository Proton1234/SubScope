/*
 * SearchPanel.tsx
 *
 * Presents the subreddit search form and reports user input back to App.
 * App owns the search state and performs the request.
 */
interface SearchPanelProps {
  subredditName: string;
  loading: boolean;
  onSubredditNameChange: (value: string) => void;
  onSearch: () => void;
}

export function SearchPanel({
  subredditName,
  loading,
  onSubredditNameChange,
  onSearch
}: SearchPanelProps) {
  return (
    <section className="search-panel" aria-label="Subreddit search">
      <div>
        <p className="eyebrow">SubScope</p>
        <h1>Community analytics from live Reddit data</h1>
        <p className="hero-copy">
          Type a community name to save its profile and inspect recent post engagement.
        </p>
        <div className="flow-steps" aria-label="Workflow">
          <span><strong>1</strong> Search</span>
          <span><strong>2</strong> Save</span>
          <span><strong>3</strong> Analyze</span>
        </div>
      </div>

      <form
        className="search-actions"
        onSubmit={(event) => {
          event.preventDefault();
          onSearch();
        }}
      >
        <label htmlFor="subreddit-search">Subreddit</label>
        <div className="prefixed-input">
          <span>r/</span>
          <input
            id="subreddit-search"
            type="text"
            value={subredditName}
            onChange={(event) => onSubredditNameChange(event.target.value)}
            placeholder="programming"
            aria-label="Subreddit name"
          />
        </div>
        <button type="submit" disabled={loading}>
          {loading ? 'Searching...' : 'Search community'}
        </button>
      </form>
    </section>
  );
}
