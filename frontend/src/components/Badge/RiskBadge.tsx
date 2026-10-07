import './RiskBadge.css'

interface RiskBadgeProps {
  riskLevel: string | null
  summary?: string | null
}

const LABELS: Record<string, string> = {
  Low: 'Aucune alerte détectée',
  Medium: 'À vérifier',
  High: 'Alerte majeure',
}

const TIERS: Record<string, 'good' | 'warning' | 'critical'> = {
  Low: 'good',
  Medium: 'warning',
  High: 'critical',
}

export function RiskBadge({ riskLevel, summary }: RiskBadgeProps) {
  if (!riskLevel) {
    return <span className="risk-badge risk-badge--unknown">Non analysé</span>
  }

  const tier = TIERS[riskLevel] ?? 'warning'
  const label = LABELS[riskLevel] ?? riskLevel

  return (
    <span className="risk-badge" title={summary ?? undefined}>
      <span className={`risk-badge-dot risk-badge-dot--${tier}`} aria-hidden="true" />
      {label}
    </span>
  )
}
