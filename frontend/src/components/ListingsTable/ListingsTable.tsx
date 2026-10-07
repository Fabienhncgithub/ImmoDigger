import { Link } from 'react-router-dom'
import type { ListingSummary } from '../../types'
import { ScoreBadge } from '../Badge/ScoreBadge'
import { RiskBadge } from '../Badge/RiskBadge'
import { PebBadge } from '../Badge/PebBadge'
import { formatArea, formatDate, formatPercent, getDisplayPrice } from '../../utils/format'
import { useDeleteListing } from '../../hooks/useListings'
import './ListingsTable.css'

interface ListingsTableProps {
  listings: ListingSummary[]
}

export function ListingsTable({ listings }: ListingsTableProps) {
  const deleteMutation = useDeleteListing()

  function handleDelete(listing: ListingSummary) {
    if (window.confirm(`Supprimer definitivement "${listing.title}" ?`)) {
      deleteMutation.mutate(listing.id)
    }
  }

  return (
    <div className="listings-table-wrapper">
      <table className="listings-table">
        <thead>
          <tr>
            <th aria-hidden="true"></th>
            <th>Annonce</th>
            <th>Commune</th>
            <th>Prix</th>
            <th>Logements</th>
            <th>Surface</th>
            <th>PEB</th>
            <th>Rendement</th>
            <th>Indice</th>
            <th>Vigilance</th>
            <th>Source</th>
            <th>Detectee le</th>
            <th aria-hidden="true"></th>
          </tr>
        </thead>
        <tbody>
          {listings.map((listing) => {
            const price = getDisplayPrice(listing.saleType, listing.askingPrice, listing.currentBid)

            return (
              <tr key={listing.id}>
                <td>
                  {listing.imageUrl ? (
                    <img className="listings-table-thumbnail" src={listing.imageUrl} alt="" loading="lazy" />
                  ) : (
                    <span className="listings-table-thumbnail listings-table-thumbnail--empty" aria-hidden="true" />
                  )}
                </td>
                <td>
                  <Link to={`/listings/${listing.id}`} className="listings-table-title">
                    {listing.title}
                  </Link>
                </td>
                <td>
                  {listing.city} ({listing.postalCode})
                </td>
                <td className="listings-table-numeric">
                  {price.value}
                  {price.isAuction && <span className="listings-table-price-label"> ({price.label})</span>}
                </td>
                <td className="listings-table-numeric">{listing.unitCount ?? '—'}</td>
                <td className="listings-table-numeric">{formatArea(listing.livingArea)}</td>
                <td>
                  <PebBadge pebRating={listing.pebRating} />
                </td>
                <td className="listings-table-numeric">{formatPercent(listing.estimatedGrossYield)}</td>
                <td>
                  <ScoreBadge score={listing.opportunityScore} criteriaCount={listing.indexCriteriaCount} />
                </td>
                <td>
                  <RiskBadge riskLevel={listing.riskLevel} summary={listing.riskSummary} />
                </td>
                <td>{listing.source}</td>
                <td>{formatDate(listing.firstSeenAt)}</td>
                <td>
                  <button
                    type="button"
                    className="listings-table-delete"
                    onClick={() => handleDelete(listing)}
                    disabled={deleteMutation.isPending}
                    aria-label="Supprimer cette annonce"
                    title="Pas interessant - supprimer"
                  >
                    ×
                  </button>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
