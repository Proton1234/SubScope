/*
 * PrivacyPage.tsx
 *
 * Public privacy policy for SubScope's current data access and storage behavior.
 */
const effectiveDate = 'July 28, 2026';

export function PrivacyPage() {
  return (
    <main className="app-shell privacy-shell">
      <section className="panel privacy-panel">
        <a className="back-link" href="/">
          Back to dashboard
        </a>
        <p className="eyebrow">SubScope</p>
        <h1>Privacy Policy</h1>
        <p className="panel-subtitle">Effective date: {effectiveDate}</p>

        <div className="privacy-content">
          <section>
            <h2>Overview</h2>
            <p>
              SubScope is a noncommercial portfolio project that analyzes public Reddit communities.
              It is independent and is not affiliated with, endorsed by, or sponsored by Reddit.
            </p>
          </section>

          <section>
            <h2>What SubScope Accesses From Reddit</h2>
            <ul>
              <li>Public subreddit metadata, such as community name, title, description, and creation date.</li>
              <li>Subscriber counts and active-account counts when Reddit makes them available.</li>
              <li>Public recent-post metadata from a subreddit&apos;s hot listing, used for aggregate analytics.</li>
            </ul>
          </section>

          <section>
            <h2>What SubScope Stores</h2>
            <ul>
              <li>Public subreddit metadata for communities searched or refreshed in the app.</li>
              <li>Aggregate engagement metrics calculated from public recent-post metadata.</li>
              <li>Timestamped subreddit-level history snapshots for trend analysis.</li>
            </ul>
          </section>

          <section>
            <h2>What SubScope Does Not Collect</h2>
            <ul>
              <li>Reddit passwords.</li>
              <li>Private messages.</li>
              <li>Private subreddit data.</li>
              <li>Individual voting history.</li>
              <li>Payment information.</li>
            </ul>
          </section>

          <section>
            <h2>How Reddit Data Is Used</h2>
            <p>
              Reddit data displayed or stored by SubScope is used to show subreddit profiles,
              engagement summaries, top-post context, and historical subreddit-level trends.
              It is not sold, used for advertising, or used to train AI models.
            </p>
          </section>

          <section>
            <h2>Data Retention And Deletion</h2>
            <p>
              Historical public subreddit metrics may be retained for trend analysis. For deletion
              or privacy requests, contact the project maintainer through the GitHub repository.
            </p>
          </section>
        </div>
      </section>
    </main>
  );
}
