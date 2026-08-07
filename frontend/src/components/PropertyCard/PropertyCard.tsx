import type { MouseEvent } from 'react'
import { Link } from 'react-router-dom'
import type { ListingSummary } from '../../types'
import { ScoreBadge } from '../Badge/ScoreBadge'
import { RiskBadge } from '../Badge/RiskBadge'
import { PebBadge } from '../Badge/PebBadge'
import { formatArea, formatDate, formatPercent, getDisplayPrice } from '../../utils/format'
import { useDeleteListing } from '../../hooks/useListings'
import './PropertyCard.css'

interface PropertyCardProps {
  listing: ListingSummary
}

export function PropertyCard({ listing }: PropertyCardProps) {
  const price = getDisplayPrice(listing.saleType, listing.askingPrice, listing.currentBid)
  const deleteMutation = useDeleteListing()

  function handleDelete(event: MouseEvent) {
    event.preventDefault() // the whole card is a <Link>; don't navigate on delete
    event.stopPropagation()
    if (window.confirm(`Supprimer definitivement "${listing.title}" ?`)) {
      deleteMutation.mutate(listing.id)
    }
  }

  return (
    <Link to={`/listings/${listing.id}`} className="property-card">
      {listing.imageUrl && (
        <img className="property-card-image" src={listing.imageUrl} alt="" loading="lazy" />
      )}

      <div className="property-card-body">
        <div className="property-card-header">
          <span className="property-card-source">{listing.source}</span>
          <div className="property-card-header-actions">
            {!listing.isActive && <span className="property-card-inactive">Inactive</span>}
            <button
              type="button"
              className="property-card-delete"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              aria-label="Supprimer cette annonce"
              title="Pas interessant - supprimer"
            >
              ×
            </button>
          </div>
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
