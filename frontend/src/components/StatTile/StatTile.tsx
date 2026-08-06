import './StatTile.css'

interface StatTileProps {
  label: string
  value: string
  tone?: 'neutral' | 'good' | 'warning' | 'critical'
}

/** A single dashboard KPI. Tone is a subtle left accent only - the value itself always stays in ink. */
export function StatTile({ label, value, tone = 'neutral' }: StatTileProps) {
  return (
    <div className={`stat-tile stat-tile--${tone}`}>
      <span className="stat-tile-label">{label}</span>
      <span className="stat-tile-value">{value}</span>
    </div>
  )
}
