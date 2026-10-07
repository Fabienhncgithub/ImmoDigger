import './ScoreBadge.css'

interface ScoreBadgeProps {
  score: number | null
  /** How many of the six criteria the index rests on, shown next to it when known. */
  criteriaCount?: number | null
}

const TOTAL_CRITERIA = 6

function scoreTier(score: number): 'good' | 'warning' | 'critical' {
  if (score >= 70) return 'good'
  if (score >= 40) return 'warning'
  return 'critical'
}

/**
 * The comparison index is shown as text in the normal ink color, with a small colored dot
 * carrying the tier - not colored text/fill, which would fail contrast for
 * the warning/serious status hues (see the dataviz skill's status palette).
 */
export function ScoreBadge({ score, criteriaCount }: ScoreBadgeProps) {
  if (score === null) {
    return <span className="score-badge score-badge--unknown">Indice non calculé</span>
  }

  return (
    <span className="score-badge">
      <span className={`score-badge-dot score-badge-dot--${scoreTier(score)}`} aria-hidden="true" />
      Indice {Math.round(score)}/100
      {criteriaCount != null && (
        <span
          className="score-badge-criteria"
          title={`Indice calculé sur ${criteriaCount} critère${criteriaCount > 1 ? 's' : ''} sur ${TOTAL_CRITERIA}`}
        >
          {criteriaCount}/{TOTAL_CRITERIA} critères
        </span>
      )}
    </span>
  )
}
