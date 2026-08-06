import './PebBadge.css'

interface PebBadgeProps {
  pebRating: string | null
}

/** Groups the A-G PEB scale into the same three status tiers as the other badges. */
function pebTier(rating: string): 'good' | 'warning' | 'critical' {
  if (rating === 'A' || rating === 'B') return 'good'
  if (rating === 'F' || rating === 'G') return 'critical'
  return 'warning'
}

export function PebBadge({ pebRating }: PebBadgeProps) {
  if (!pebRating) {
    return <span className="peb-badge peb-badge--unknown">PEB : —</span>
  }

  return (
    <span className="peb-badge">
      <span className={`peb-badge-dot peb-badge-dot--${pebTier(pebRating)}`} aria-hidden="true" />
      PEB {pebRating}
    </span>
  )
}
