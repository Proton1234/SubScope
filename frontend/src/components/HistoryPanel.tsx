/*
 * HistoryPanel.tsx
 *
 * Renders historical subscriber snapshots returned by the backend.
 * This first slice charts subscriber count only.
 */
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';
import { SubredditHistorySnapshot } from '../types/api';

interface HistoryPanelProps {
  history: SubredditHistorySnapshot[];
  loading: boolean;
  error: string | null;
}

interface HistoryPoint {
  label: string;
  fullDate: string;
  subscriberCount: number;
}

function formatCompactNumber(value: number): string {
  return new Intl.NumberFormat(undefined, {
    notation: 'compact',
    maximumFractionDigits: 1
  }).format(value);
}

function formatSnapshotDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric'
  });
}

export function HistoryPanel({ history, loading, error }: HistoryPanelProps) {
  const chartData: HistoryPoint[] = history.map((snapshot) => ({
    label: formatSnapshotDate(snapshot.capturedAtUtc),
    fullDate: new Date(snapshot.capturedAtUtc).toLocaleString(),
    subscriberCount: snapshot.subscriberCount
  }));

  return (
    <section className="panel history-panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Subscriber history</p>
          <h2>Growth snapshots</h2>
        </div>
        {history.length > 0 && <span className="count-pill">{history.length} snapshots</span>}
      </div>

      {loading && <p className="muted">Loading subscriber history...</p>}
      {error && <p className="error-message">{error}</p>}
      {!loading && !error && history.length === 0 && (
        <div className="empty-insights compact-empty">
          <strong>No history yet</strong>
          <p>Search this community again later to build a subscriber trend.</p>
        </div>
      )}
      {!loading && !error && history.length > 0 && (
        <div className="history-chart" aria-label="Subscriber history line chart">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData} margin={{ top: 12, right: 16, bottom: 4, left: 0 }}>
              <CartesianGrid stroke="#e8edf5" strokeDasharray="4 4" />
              <XAxis dataKey="label" tickLine={false} axisLine={false} />
              <YAxis
                tickFormatter={formatCompactNumber}
                tickLine={false}
                axisLine={false}
                width={56}
                domain={['dataMin', 'dataMax']}
              />
              <Tooltip
                formatter={(value) => [Number(value).toLocaleString(), 'Subscribers']}
                labelFormatter={(_, payload) => payload?.[0]?.payload?.fullDate ?? ''}
              />
              <Line
                type="monotone"
                dataKey="subscriberCount"
                stroke="#ff4500"
                strokeWidth={3}
                dot={{ r: 4, fill: '#ffffff', stroke: '#ff4500', strokeWidth: 2 }}
                activeDot={{ r: 6 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </section>
  );
}
