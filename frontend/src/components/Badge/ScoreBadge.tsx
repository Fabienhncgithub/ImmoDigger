import './ScoreBadge.css'

interface ScoreBadgeProps {
  score: number | null
}

function scoreTier(score: number): 'good' | 'warning' | 'critical' {
  if (score >= 70) return 'good'
  if (score >= 40) return 'warning'
  return 'critical'
}

/**
 * Score is shown as text in the normal ink color, with a small colored dot
 * carrying the tier - not colored text/fill, which would fail contrast for
 * the warning/serious status hues (see the dataviz skill's status palette).
 */
export function ScoreBadge({ score }: ScoreBadgeProps) {
  if (score === null) {
    return <span className="score-badge score-badge--unknown">Score : —</span>
  }

  return (
    <span className="score-badge">
      <span className={`score-badge-dot score-badge-dot--${scoreTier(score)}`} aria-hidden="true" />
      {Math.round(score)}/100
    </span>
  )
}
