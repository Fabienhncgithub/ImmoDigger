import { Link } from 'react-router-dom'
import type { ListingSummary } from '../../types'
import { formatArea, formatDate, formatPercent, getMarketAge, formatPrice, formatPropertyType, getDisplayPrice } from '../../utils/format'
import { useDeleteListing } from '../../hooks/useListings'
import { AppIcon } from '../Icon/AppIcon'
import { ScoreBadge } from '../Badge/ScoreBadge'
import { safeExternalUrl } from '../../utils/safeUrl'
import './PropertyCard.css'

interface PropertyCardProps {
  listing: ListingSummary
}

export function PropertyCard({ listing }: PropertyCardProps) {
  const price = getDisplayPrice(listing.saleType, listing.askingPrice, listing.currentBid)
  const deleteMutation = useDeleteListing()
  const mediaStatus = getMediaStatus(listing.imageUrl)
  const hasPropertyImage = Boolean(listing.imageUrl && !mediaStatus)
  const displayTitle = listing.title.replace(/^Bien Proximus\s*-\s*/i, '')
  const displayPrice = price.value === '—' ? 'Prix sur demande' : price.value
  const sourceName = formatSourceName(listing.source)
  const location = [listing.address, [listing.postalCode, listing.city].filter(Boolean).join(' ')]
    .filter(Boolean)
    .join(', ')
  const facts = getFacts(listing)
  const urbanisticFact = getUrbanisticFact(listing)
  const marketAge = getMarketAge(listing.listedSince)
  const description = getDescriptionExcerpt(listing.description, listing.title)

  function handleDelete() {
    if (window.confirm(`Supprimer définitivement « ${listing.title} » ?`)) {
      deleteMutation.mutate(listing.id)
    }
  }

  return (
    <article className="property-card">
      <Link
        to={`/listings/${listing.id}`}
        className={`property-card-media${hasPropertyImage ? '' : ' property-card-media--graphic'}`}
        aria-label={`Ouvrir la fiche ImmoDigger : ${displayTitle}`}
      >
        {hasPropertyImage && (
          <img className="property-card-image" src={listing.imageUrl!} alt="" loading="lazy" />
        )}

        {!hasPropertyImage && (
          <div className="property-card-placeholder" aria-hidden="true">
            <span className="property-card-building-line property-card-building-line--one" />
            <span className="property-card-building-line property-card-building-line--two" />
            <span className="property-card-building-line property-card-building-line--three" />
            <AppIcon name="building" />
          </div>
        )}

        <div className="property-card-media-shade" />
        <div className="property-card-media-topline">
          <span className="property-card-source">{sourceName}</span>
          <span className="property-card-media-labels">
            {listing.opportunityScore !== null && (
              <ScoreBadge score={listing.opportunityScore} criteriaCount={listing.indexCriteriaCount} />
            )}
            {marketAge.tone !== 'recent' && (
              <span className={`property-card-age property-card-age--${marketAge.tone}`}>
                En vente depuis {marketAge.duration}
              </span>
            )}
            {listing.isDemo && <span className="property-card-demo">Démo</span>}
            {mediaStatus && <span className="property-card-status">{mediaStatus}</span>}
            {!listing.isActive && <span className="property-card-inactive">Archivé</span>}
          </span>
        </div>
        <span className="property-card-type">{formatPropertyType(listing.propertyType)}</span>
      </Link>

      <div className="property-card-body">
        <div className="property-card-heading">
          <p className="property-card-price">
            {displayPrice}
            {price.isAuction && <span className="property-card-price-label"> {price.label}</span>}
          </p>
          <button
            type="button"
            className="property-card-delete"
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            aria-label="Supprimer cette annonce"
            title="Pas intéressant — supprimer"
          >
            <AppIcon name="trash" />
          </button>
        </div>

        <Link to={`/listings/${listing.id}`} className="property-card-title-link">
          <h3 className="property-card-title">{displayTitle}</h3>
        </Link>

        <p className="property-card-location">
          <AppIcon name="pin" />
          {location || 'Localisation non précisée'}
        </p>

        {(facts.length > 0 || urbanisticFact) && (
          <ul className="property-card-facts" aria-label="Caractéristiques principales">
            {facts.map((fact) => <li key={fact}>{fact}</li>)}
            {urbanisticFact && (
              <li className={`property-card-fact--${urbanisticFact.tone}`}>{urbanisticFact.label}</li>
            )}
          </ul>
        )}

        {description && <p className="property-card-description">{description}</p>}

        {listing.riskLevel && listing.riskLevel !== 'Low' && listing.riskSummary && (
          <p className="property-card-attention">
            <strong>À vérifier :</strong> {getVigilanceReason(listing.riskSummary)}
          </p>
        )}

        <div className="property-card-meta">
          <span title={`Première détection le ${formatDate(listing.firstSeenAt)}`}>
            {marketAge.days === 0 ? 'Nouveau aujourd’hui' : `En ligne depuis ${marketAge.duration}`}
          </span>
          {listing.officialDocumentCount > 0 && (
            <span>{listing.officialDocumentCount} document{listing.officialDocumentCount > 1 ? 's' : ''}</span>
          )}
        </div>

        <div className="property-card-actions">
          <a
            href={safeExternalUrl(listing.url)}
            target="_blank"
            rel="noreferrer"
            className="property-card-primary-action"
          >
            Voir sur {sourceName}
            <span aria-hidden="true">↗</span>
          </a>
          <Link to={`/listings/${listing.id}`} className="property-card-secondary-action">
            Voir la fiche
            <AppIcon name="arrow" />
          </Link>
        </div>
      </div>
    </article>
  )
}

function getFacts(listing: ListingSummary): string[] {
  const facts = [
    listing.bedroomCount === null
      ? null
      : `${listing.bedroomCount} chambre${listing.bedroomCount > 1 ? 's' : ''}`,
    listing.unitCount === null
      ? null
      : `${listing.unitCount} logement${listing.unitCount > 1 ? 's' : ''}`,
    listing.livingArea === null ? null : formatArea(listing.livingArea),
    getPricePerSquareMeter(listing),
    listing.estimatedGrossYield === null ? null : `Rendement ${formatPercent(listing.estimatedGrossYield)}`,
    listing.landArea === null ? null : `${formatArea(listing.landArea)} de terrain`,
    listing.pebRating ? `PEB ${listing.pebRating}` : null,
  ]

  return facts.filter((fact): fact is string => fact !== null)
}

/** Price per m² of living area: the quickest way to compare two listings at a glance. */
function getPricePerSquareMeter(listing: ListingSummary): string | null {
  const referencePrice = listing.saleType === 'PublicSale'
    ? listing.currentBid ?? listing.askingPrice
    : listing.askingPrice
  if (!referencePrice || !listing.livingArea) return null

  return `${formatPrice(referencePrice / listing.livingArea)}/m²`
}

/** Only shown when the listing text actually says something: "to be checked" is the default and would be noise on every card. */
function getUrbanisticFact(listing: ListingSummary): { label: string; tone: 'critical' | 'good' } | null {
  if (listing.urbanisticStatus === 'Infraction') return { label: 'Urbanisme : à régulariser', tone: 'critical' }
  if (listing.urbanisticStatus === 'Compliant') return { label: 'Urbanisme : sans infraction annoncée', tone: 'good' }
  return null
}

function getDescriptionExcerpt(description: string, title: string): string | null {
  let text = description.replace(/\s+/g, ' ').trim()
  if (!text) return null

  if (text.toLocaleLowerCase('fr').startsWith(title.trim().toLocaleLowerCase('fr'))) {
    text = text.slice(title.trim().length).trim()
  }

  text = text.replace(/\bVoir (?:les )?photos\s*→?\s*$/i, '').trim()
  const prose = text
    .replace(/\d+\s*chambres?/gi, '')
    .replace(/\d[\d.,\s]*\s*m(?:²|2)/gi, '')
    .replace(/€\s*[\d.,\s]+/g, '')
    .replace(/[·|–—,.:;\s]/g, '')

  return prose.length >= 12 ? text : null
}

function getMediaStatus(imageUrl: string | null): string | null {
  if (!imageUrl) return null
  if (/in[-_]?option|inoption/i.test(imageUrl)) return 'Sous option'
  if (/sold|vendu|verkocht/i.test(imageUrl)) return 'Vendu'
  return null
}

function formatSourceName(source: string): string {
  const labels: Record<string, string> = {
    ProximusRealEstate: 'Proximus Real Estate',
    RegieDesBatiments: 'Régie des Bâtiments',
    BpostImmo: 'bpost immobilier',
  }

  return labels[source] ?? source
}

function getVigilanceReason(summary: string): string {
  return summary.replace(/^[^:]+:\s*/, '')
}
