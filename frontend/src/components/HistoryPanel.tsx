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
  fullDate: string;
  capturedAtUtc: string;
  index: number;
  subscriberCount: number;
}

function getSnapshotTicks(points: HistoryPoint[], maxTicks = 5): number[] {
  if (points.length <= maxTicks) {
    return points.map((point) => point.index);
  }

  const ticks = new Set<number>();
  const lastIndex = points.length - 1;
  ticks.add(0);
  ticks.add(lastIndex);

  for (let index = 0; index < maxTicks; index += 1) {
    const pointIndex = Math.round((index * lastIndex) / (maxTicks - 1));
    ticks.add(points[pointIndex].index);
  }

  return Array.from(ticks).sort((left, right) => left - right);
}

function formatCompactNumber(value: number, maximumFractionDigits = 1): string {
  return new Intl.NumberFormat(undefined, {
    notation: 'compact',
    maximumFractionDigits
  }).format(value);
}

function formatAxisTimestamp(value: string): string {
  return new Date(value).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  });
}

function formatGrowth(value: number): string {
  const sign = value > 0 ? '+' : '';
  return `${sign}${formatCompactNumber(value, 2)}`;
}

function formatSnapshotCount(count: number): string {
  return `${count} snapshot${count === 1 ? '' : 's'}`;
}

function HistoryTooltip({ active, payload }: { active?: boolean; payload?: Array<{ payload?: HistoryPoint }> }) {
  const point = payload?.find((entry) => entry.payload)?.payload;

  if (!active || !point) {
    return null;
  }

  return (
    <div
      style={{
        background: '#ffffff',
        border: '1px solid #d7dde8',
        color: '#17202a',
        padding: '0.7rem 0.8rem'
      }}
    >
      <div>{point.fullDate}</div>
      <div style={{ color: '#ff4500', marginTop: '0.35rem' }}>
        Subscribers: {point.subscriberCount.toLocaleString()}
      </div>
    </div>
  );
}

export function HistoryPanel({ history, loading, error }: HistoryPanelProps) {
  const chartData: HistoryPoint[] = history
    .map((snapshot) => ({
      fullDate: new Date(snapshot.capturedAtUtc).toLocaleString(),
      capturedAtUtc: snapshot.capturedAtUtc,
      index: 0,
      subscriberCount: snapshot.subscriberCount
    }))
    .sort((left, right) => Date.parse(left.capturedAtUtc) - Date.parse(right.capturedAtUtc))
    .map((point, index) => ({ ...point, index }));
  const firstSubscriberCount = chartData[0]?.subscriberCount ?? 0;
  const firstSnapshot = chartData[0];
  const xAxisTicks = getSnapshotTicks(chartData);

  return (
    <section className="panel history-panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Subscriber history</p>
          <h2>Subscriber growth since tracking began</h2>
        </div>
        {history.length > 0 && <span className="count-pill">{formatSnapshotCount(history.length)}</span>}
      </div>

      {loading && <p className="muted">Loading subscriber history...</p>}
      {error && <p className="error-message">{error}</p>}
      {!loading && !error && history.length === 0 && (
        <div className="empty-insights compact-empty">
          <strong>No subscriber history yet.</strong>
          <p>Search this community to start collecting subscriber snapshots.</p>
        </div>
      )}
      {!loading && !error && history.length === 1 && firstSnapshot && (
        <div className="empty-insights compact-empty">
          <strong>Tracking started</strong>
          <p>
            The first subscriber snapshot was collected at {firstSnapshot.fullDate}. Return later to see growth over
            time.
          </p>
          <div className="metric-grid tracking-summary-grid">
            <div className="metric-card">
              <span>Current subscribers</span>
              <strong>{firstSnapshot.subscriberCount.toLocaleString()}</strong>
            </div>
            <div className="metric-card">
              <span>First snapshot</span>
              <strong>{firstSnapshot.fullDate}</strong>
            </div>
            <div className="metric-card">
              <span>Snapshots collected</span>
              <strong>{formatSnapshotCount(history.length)}</strong>
            </div>
          </div>
        </div>
      )}
      {!loading && !error && history.length === 2 && firstSnapshot && (
        <div className="empty-insights compact-empty">
          <strong>Tracking in progress</strong>
          <p>
            2 snapshots collected since {firstSnapshot.fullDate}. More data is needed before a meaningful trend can be
            shown.
          </p>
          <div className="metric-grid tracking-summary-grid">
            <div className="metric-card">
              <span>Starting subscribers</span>
              <strong>{firstSnapshot.subscriberCount.toLocaleString()}</strong>
            </div>
            <div className="metric-card">
              <span>Latest subscribers</span>
              <strong>{chartData[1].subscriberCount.toLocaleString()}</strong>
            </div>
            <div className="metric-card">
              <span>Snapshots collected</span>
              <strong>{formatSnapshotCount(history.length)}</strong>
            </div>
          </div>
        </div>
      )}
      {!loading && !error && history.length >= 3 && (
        <div className="history-chart" aria-label="Subscriber history line chart">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData} margin={{ top: 12, right: 16, bottom: 4, left: 0 }}>
              <CartesianGrid stroke="#e8edf5" strokeDasharray="4 4" />
              <XAxis
                dataKey="index"
                ticks={xAxisTicks}
                tickFormatter={(value) => formatAxisTimestamp(chartData[Number(value)]?.capturedAtUtc ?? '')}
                tickLine={false}
                axisLine={false}
                tickMargin={8}
              />
              <YAxis
                dataKey="subscriberCount"
                tickFormatter={(value) => formatGrowth(Number(value) - firstSubscriberCount)}
                tickLine={false}
                axisLine={false}
                width={72}
                domain={['dataMin', 'dataMax']}
              />
              <Tooltip content={<HistoryTooltip />} />
              <Line
                type="monotone"
                dataKey="subscriberCount"
                stroke="#ff4500"
                strokeWidth={3}
                dot={false}
                activeDot={{ r: 6, fill: '#ffffff', stroke: '#ff4500', strokeWidth: 2 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </section>
  );
}
