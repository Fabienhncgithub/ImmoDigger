import { Link } from 'react-router-dom'
import type { ListingSummary } from '../../types'
import { ScoreBadge } from '../Badge/ScoreBadge'
import { RiskBadge } from '../Badge/RiskBadge'
import { PebBadge } from '../Badge/PebBadge'
import { formatArea, formatDate, formatPercent, getDisplayPrice } from '../../utils/format'
import './PropertyCard.css'

interface PropertyCardProps {
  listing: ListingSummary
}

export function PropertyCard({ listing }: PropertyCardProps) {
  const price = getDisplayPrice(listing.saleType, listing.askingPrice, listing.currentBid)

  return (
    <Link to={`/listings/${listing.id}`} className="property-card">
      {listing.imageUrl && (
        <img className="property-card-image" src={listing.imageUrl} alt="" loading="lazy" />
      )}

      <div className="property-card-body">
        <div className="property-card-header">
          <span className="property-card-source">{listing.source}</span>
          {!listing.isActive && <span className="property-card-inactive">Inactive</span>}
        </div>

        <h3 className="property-card-title">{listing.title}</h3>
        <p className="property-card-location">
          {listing.city} ({listing.postalCode})
        </p>

        <p className="property-card-price">
          {price.value}
          <span className="property-card-price-label">
            {price.isAuction && ' · '}
            {price.isAuction && price.label}
          </span>
        </p>

        <dl className="property-card-facts">
          <div>
            <dt>Logements</dt>
            <dd>{listing.unitCount ?? '—'}</dd>
          </div>
          <div>
            <dt>Surface</dt>
            <dd>{formatArea(listing.livingArea)}</dd>
          </div>
          <div>
            <dt>Rendement</dt>
            <dd>{formatPercent(listing.estimatedGrossYield)}</dd>
          </div>
        </dl>

        <div className="property-card-badges">
          <ScoreBadge score={listing.opportunityScore} />
          <RiskBadge riskLevel={listing.riskLevel} />
          <PebBadge pebRating={listing.pebRating} />
        </div>

        <p className="property-card-date">Detecte le {formatDate(listing.firstSeenAt)}</p>
      </div>
    </Link>
  )
}
