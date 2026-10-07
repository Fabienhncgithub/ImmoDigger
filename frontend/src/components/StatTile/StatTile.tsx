import { Link } from 'react-router-dom'
import { AppIcon, type AppIconName } from '../Icon/AppIcon'
import './StatTile.css'

interface StatTileProps {
  label: string
  value: string
  tone?: 'neutral' | 'good' | 'warning' | 'critical'
  icon: AppIconName
  hint: string
  featured?: boolean
  /** When set, the whole tile links to the listings behind the figure. */
  to?: string
}

/** A decision-oriented dashboard KPI with restrained semantic colour. */
export function StatTile({
  label,
  value,
  tone = 'neutral',
  icon,
  hint,
  featured = false,
  to,
}: StatTileProps) {
  const className = `stat-tile stat-tile--${tone}${featured ? ' stat-tile--featured' : ''}`
  const content = (
    <>
      <div className="stat-tile-topline">
        <span className="stat-tile-label">{label}</span>
        <span className="stat-tile-icon">
          <AppIcon name={icon} />
        </span>
      </div>
      <span className="stat-tile-value">{value}</span>
      <span className="stat-tile-hint">{hint}</span>
    </>
  )

  if (to) {
    return (
      <Link to={to} className={`${className} stat-tile--link`}>
        {content}
      </Link>
    )
  }

  return <div className={className}>{content}</div>
}
