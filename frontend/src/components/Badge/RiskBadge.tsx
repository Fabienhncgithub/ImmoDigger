import './RiskBadge.css'

interface RiskBadgeProps {
  riskLevel: string | null
}

const LABELS: Record<string, string> = {
  Low: 'Risque faible',
  Medium: 'Risque modere',
  High: 'Risque eleve',
}

const TIERS: Record<string, 'good' | 'warning' | 'critical'> = {
  Low: 'good',
  Medium: 'warning',
  High: 'critical',
}

export function RiskBadge({ riskLevel }: RiskBadgeProps) {
  if (!riskLevel) {
    return <span className="risk-badge risk-badge--unknown">Risque : —</span>
  }

  const tier = TIERS[riskLevel] ?? 'warning'
  const label = LABELS[riskLevel] ?? riskLevel

  return (
    <span className="risk-badge">
      <span className={`risk-badge-dot risk-badge-dot--${tier}`} aria-hidden="true" />
      {label}
    </span>
  )
}
