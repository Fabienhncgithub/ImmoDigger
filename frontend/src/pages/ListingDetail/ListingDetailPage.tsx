import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  useAnalyzeListing,
  useDeleteListing,
  useListing,
  useMarkReviewed,
  usePriceHistory,
  useUpdateListing,
} from '../../hooks/useListings'
import {
  formatArea,
  formatDate,
  formatDateTime,
  formatPercent,
  formatPrice,
  formatPropertyType,
  formatSaleType,
  formatUrbanisticStatus,
  getMarketAge,
  getDisplayPrice,
} from '../../utils/format'
import type { OpportunityScoreBreakdown } from '../../types'
import { safeExternalUrl } from '../../utils/safeUrl'
import './ListingDetailPage.css'

const documentTypeLabels: Record<string, string> = {
  URBANISM: 'Urbanisme',
  PROPERTY_TAX: 'Cadastre',
  PEB: 'PEB / énergie',
  ELECTRICAL_INSTALLATION: 'Installation électrique',
  SOIL_CERTIFICATE: 'Attestation du sol',
  FLOOD_ZONE: 'Zone inondable',
  OIL_TANK: 'Citerne à mazout',
  ASBESTOS_CERTIFICATE: 'Amiante',
  CO_OWNERSHIP: 'Copropriété',
  PLAN: 'Plan',
  MISC: 'Autre document',
}

function getReadableDescription(description: string, title: string): string | null {
  let text = description.replace(/\s+/g, ' ').trim()
  if (!text) return null

  if (text.toLocaleLowerCase('fr').startsWith(title.trim().toLocaleLowerCase('fr'))) {
    text = text.slice(title.trim().length).trim()
  }

  text = text.replace(/\bVoir (?:les )?photos\s*→?\s*$/i, '').trim()
  return text || null
}

export function ListingDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: listing, isLoading, isError } = useListing(id)
  const { data: priceHistory } = usePriceHistory(id)
  const analyzeMutation = useAnalyzeListing(id ?? '')
  const updateMutation = useUpdateListing(id ?? '')
  const markReviewedMutation = useMarkReviewed(id ?? '')
  const deleteMutation = useDeleteListing()

  const [notes, setNotes] = useState('')
  const [rent, setRent] = useState('')
  const [acquisitionCosts, setAcquisitionCosts] = useState('')
  const [renovationBudget, setRenovationBudget] = useState('')
  const [breakdown, setBreakdown] = useState<OpportunityScoreBreakdown | null>(null)

  useEffect(() => {
    if (!listing) return
    setNotes(listing.personalNotes ?? '')
    setRent(listing.estimatedMonthlyRentPerUnit?.toString() ?? '')
    setAcquisitionCosts(listing.estimatedAcquisitionCosts?.toString() ?? '')
    setRenovationBudget(listing.estimatedRenovationBudget?.toString() ?? '')
  }, [listing])

  if (isLoading) return <p className="listing-detail-status">Chargement...</p>
  if (isError || !listing) return <p className="listing-detail-status">Annonce introuvable.</p>

  function handleSave() {
    updateMutation.mutate({
      personalNotes: notes || null,
      estimatedMonthlyRentPerUnit: rent ? Number(rent) : null,
      estimatedAcquisitionCosts: acquisitionCosts ? Number(acquisitionCosts) : null,
      estimatedRenovationBudget: renovationBudget ? Number(renovationBudget) : null,
    })
  }

  async function handleAnalyze() {
    const response = await analyzeMutation.mutateAsync()
    setBreakdown(response.scoreBreakdown)
  }

  function handleDelete() {
    if (!listing) return
    if (window.confirm(`Supprimer definitivement "${listing.title}" ?`)) {
      deleteMutation.mutate(listing.id, { onSuccess: () => navigate('/listings') })
    }
  }

  const price = getDisplayPrice(listing.saleType, listing.askingPrice, listing.currentBid)
  const readableDescription = getReadableDescription(listing.description, listing.title)
  const amenities = [
    listing.hasGarage ? 'Garage' : null,
    listing.hasTerrace ? 'Terrasse' : null,
    listing.hasGarden ? 'Jardin' : null,
  ].filter((value): value is string => value !== null)
  const marketAge = getMarketAge(listing.listedSince)
  const informationFacts = [
    listing.askingPrice !== null
      ? { label: listing.saleType === 'PublicSale' ? 'Mise à prix' : 'Prix demandé', value: formatPrice(listing.askingPrice) }
      : null,
    listing.saleType === 'PublicSale' && listing.currentBid !== null
      ? { label: 'Enchère actuelle', value: formatPrice(listing.currentBid) }
      : null,
    { label: 'Type de vente', value: formatSaleType(listing.saleType) },
    { label: 'Type de bien', value: formatPropertyType(listing.propertyType) },
    listing.bedroomCount !== null || listing.bathroomCount !== null
      ? { label: 'Chambres / salles de bain', value: `${listing.bedroomCount ?? 'Non indiqué'} / ${listing.bathroomCount ?? 'Non indiqué'}` }
      : null,
    listing.livingArea !== null ? { label: 'Surface habitable', value: formatArea(listing.livingArea) } : null,
    listing.landArea !== null ? { label: 'Surface du terrain', value: formatArea(listing.landArea) } : null,
    listing.electricalInstallationCompliant !== null
      ? { label: 'Installation électrique', value: listing.electricalInstallationCompliant ? 'Conforme' : 'Non conforme' }
      : null,
    { label: 'Urbanisme, d’après le texte de l’annonce', value: formatUrbanisticStatus(listing.urbanisticStatus) },
    listing.isOccupied !== null ? { label: 'Occupation', value: listing.isOccupied ? 'Occupé' : 'Libre' } : null,
    amenities.length > 0 ? { label: 'Équipements', value: amenities.join(', ') } : null,
    listing.cadastralIncome !== null ? { label: 'Revenu cadastral', value: formatPrice(listing.cadastralIncome) } : null,
    { label: 'Première détection', value: formatDate(listing.firstSeenAt) },
    {
      label: 'En vente depuis',
      value: marketAge.tone === 'recent'
        ? marketAge.duration
        : `${marketAge.duration} — ${marketAge.tone === 'stale' ? 'depuis longtemps, marge de négociation probable' : 'commence à dater'}`,
    },
  ].filter((fact): fact is { label: string; value: string } => fact !== null)

  return (
    <section className="listing-detail-page">
      <Link to="/listings" className="listing-detail-back">
        ← Retour aux annonces
      </Link>

      {listing.isDemo && (
        <div className="listing-detail-demo-notice">
          <strong>Bien de démonstration</strong>
          <span>Cette fiche est fictive et sert uniquement à tester l’analyse ImmoDigger.</span>
        </div>
      )}

      {listing.imageUrl && (
        <img className="listing-detail-image" src={listing.imageUrl} alt="" />
      )}

      <header className="listing-detail-header">
        <div>
          <span className="listing-detail-source">{listing.source}</span>
          <h1>{listing.title}</h1>
          <p className="listing-detail-address">
            {listing.address ? `${listing.address}, ` : ''}
            {listing.postalCode} {listing.city}
          </p>
          <p className="listing-detail-price">
            {price.value}
            <span className="listing-detail-price-label">
              {price.isAuction ? ` · ${price.label}` : ''}
            </span>
          </p>
          <div className="listing-detail-main-actions">
            <a
              href={safeExternalUrl(listing.url)}
              target="_blank"
              rel="noreferrer"
              className="listing-detail-original-action"
            >
              Voir l’annonce sur {listing.source}
              <span aria-hidden="true">↗</span>
            </a>
            <a href="#description" className="listing-detail-description-shortcut">
              Lire la description
            </a>
          </div>
        </div>
      </header>

      <div className="listing-detail-grid">
        <div className="listing-detail-main">
          <section id="description" className="listing-detail-card listing-detail-description-card">
            <span className="listing-detail-eyebrow">Ce que dit l’annonce</span>
            <h2>Description</h2>
            <p className={`listing-detail-description${readableDescription ? '' : ' is-unavailable'}`}>
              {readableDescription ??
                `La source ${listing.source} ne fournit pas de description complète dans les données reçues. Consultez l’annonce originale pour lire tous les détails.`}
            </p>
          </section>

          <section className="listing-detail-card">
            <h2>En bref</h2>
            <dl className="listing-detail-facts">
              {informationFacts.map((fact) => (
                <div key={fact.label}>
                  <dt>{fact.label}</dt>
                  <dd>{fact.value}</dd>
                </div>
              ))}
            </dl>
            <p className="listing-detail-available-note">Seules les informations fournies par la source sont affichées.</p>
          </section>

          <section className="listing-detail-card listing-detail-housing-evidence">
            <div className="listing-detail-section-heading">
              <div>
                <span className="listing-detail-eyebrow">Vérification urbanistique</span>
                <h2>Nombre de logements</h2>
              </div>
              {listing.officialDocuments.length > 0 && (
                <span className="listing-detail-document-count">
                  {listing.officialDocuments.length} document{listing.officialDocuments.length > 1 ? 's' : ''}
                </span>
              )}
            </div>

            <div className="listing-detail-unit-comparison">
              <div>
                <span>Annoncé par le bien</span>
                <strong>{listing.observedUnitCount ?? 'Non précisé'}</strong>
                <small>Information descriptive, pas une preuve urbanistique.</small>
              </div>
              <div className={listing.officialUnitCount === null ? 'is-pending' : 'is-confirmed'}>
                <span>Officiellement identifié</span>
                <strong>{listing.officialUnitCount ?? 'À vérifier'}</strong>
                <small>
                  {listing.officialUnitCountSourceName ??
                    (listing.officialDocuments.length > 0
                      ? 'Les documents sont disponibles, mais aucun nombre explicite n’a été détecté.'
                      : `Aucun document officiel n’est encore publié par ${listing.source}.`)}
                </small>
              </div>
            </div>

            {listing.officialUnitCountSourceUrl && (
              <a
                href={safeExternalUrl(listing.officialUnitCountSourceUrl)}
                target="_blank"
                rel="noreferrer"
                className="listing-detail-link"
              >
                Ouvrir la preuve du nombre officiel ↗
              </a>
            )}
          </section>

          {listing.officialDocuments.length > 0 && (
            <details className="listing-detail-card listing-detail-disclosure">
              <summary>
                <span>Documents officiels ({listing.officialDocuments.length})</span>
                <span aria-hidden="true">+</span>
              </summary>
              <div className="listing-detail-disclosure-content">
                <p className="listing-detail-document-intro">
                  Ces liens ouvrent les PDF publiés directement par {listing.source}. Vérifie toujours le document
                  urbanistique avant une décision d’achat.
                </p>
                <ul className="listing-detail-document-list">
                  {listing.officialDocuments.map((document) => (
                    <li key={document.url}>
                      <span>{documentTypeLabels[document.type] ?? document.type}</span>
                      <a href={safeExternalUrl(document.url)} target="_blank" rel="noreferrer">
                        {document.name} ↗
                      </a>
                    </li>
                  ))}
                </ul>
              </div>
            </details>
          )}

          <details className="listing-detail-card listing-detail-disclosure">
            <summary>
              <span>Points de vigilance</span>
              <span aria-hidden="true">+</span>
            </summary>
            <div className="listing-detail-disclosure-content">
              <p className="listing-detail-description">
                {listing.riskSummary || "Aucune vérification automatique n’a encore été effectuée."}
              </p>
            </div>
          </details>

          {breakdown && (
            <section className="listing-detail-card">
              <h2>Indice ImmoDigger</h2>
              <p className="listing-detail-index-explanation">
                Cet indice sert à comparer les biens entre eux. Il ne représente ni une valeur de
                marché, ni une recommandation d’achat.
                {' '}
                <Link to="/indice">Comment est-il calculé ?</Link>
              </p>
              <div className="listing-detail-index-summary">
                <strong>
                  {breakdown.totalScore === null ? 'Non calculable' : `${Math.round(breakdown.totalScore)}/100`}
                </strong>
                <span>
                  Données disponibles : {Math.round(breakdown.dataCompletenessPercentage)} % ·{' '}
                  {breakdown.evaluatedCriteriaCount}/{breakdown.totalCriteriaCount} critères
                </span>
              </div>
              {breakdown.totalScore === null && (
                <p className="listing-detail-index-warning">
                  Il faut au moins 40 % des données pondérées pour afficher un indice fiable.
                </p>
              )}
              <ul className="listing-detail-score-breakdown">
                {[
                  ['Prix / m²', breakdown.pricePerSquareMeterScore, 25, breakdown.pricePerSquareMeterAvailable],
                  ['Rendement brut', breakdown.grossYieldScore, 25, breakdown.grossYieldAvailable],
                  ['Nombre de logements', breakdown.unitCountScore, 15, breakdown.unitCountAvailable],
                  ['Localisation', breakdown.locationScore, 15, breakdown.locationAvailable],
                  ['Énergie', breakdown.energyScore, 10, breakdown.energyAvailable],
                  ['Vigilance documentaire', breakdown.riskScore, 10, breakdown.riskAvailable],
                ].map(([label, value, maximum, available]) => (
                  <li key={String(label)} className={available ? '' : 'is-unavailable'}>
                    <span>{label}</span>
                    <span>{available ? `${value} / ${maximum}` : 'Non évalué'}</span>
                  </li>
                ))}
                <li className="listing-detail-score-total">
                  <span>Indice normalisé</span>
                  <span>{breakdown.totalScore === null ? '—' : `${breakdown.totalScore} / 100`}</span>
                </li>
              </ul>
              {breakdown.missingData.length > 0 && (
                <>
                  <h3>Données à compléter</h3>
                  <ul className="listing-detail-signal-list listing-detail-missing-list">
                    {breakdown.missingData.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </>
              )}
              {breakdown.positiveSignals.length > 0 && (
                <>
                  <h3>Points positifs</h3>
                  <ul className="listing-detail-signal-list">
                    {breakdown.positiveSignals.map((signal) => (
                      <li key={signal}>{signal}</li>
                    ))}
                  </ul>
                </>
              )}
              {breakdown.riskSignals.length > 0 && (
                <>
                  <h3>Points d'attention</h3>
                  <ul className="listing-detail-signal-list">
                    {breakdown.riskSignals.map((signal) => (
                      <li key={signal}>{signal}</li>
                    ))}
                  </ul>
                </>
              )}
            </section>
          )}

          <details className="listing-detail-card listing-detail-disclosure">
            <summary>
              <span>Historique des prix</span>
              <span aria-hidden="true">+</span>
            </summary>
            <div className="listing-detail-disclosure-content">
              {priceHistory && priceHistory.length > 0 ? (
                <table className="listing-detail-price-history">
                  <thead><tr><th>Date</th><th>Prix</th></tr></thead>
                  <tbody>{priceHistory.map((entry) => <tr key={entry.id}><td>{formatDate(entry.recordedAt)}</td><td>{formatPrice(entry.price)}</td></tr>)}</tbody>
                </table>
              ) : (
                <p className="listing-detail-description">Aucun historique de prix disponible.</p>
              )}
            </div>
          </details>
        </div>

        <aside className="listing-detail-sidebar">
          <section className="listing-detail-card">
            <h2>Actions</h2>
            <button type="button" className="listing-detail-primary-button" onClick={handleAnalyze}>
              {analyzeMutation.isPending ? 'Calcul...' : 'Calculer l’indice'}
            </button>
            <button
              type="button"
              className="listing-detail-secondary-button"
              onClick={() => markReviewedMutation.mutate()}
              disabled={listing.isReviewed}
            >
              {listing.isReviewed
                ? `Analyse le ${formatDateTime(listing.reviewedAt)}`
                : 'Marquer comme analyse'}
            </button>
            <button
              type="button"
              className="listing-detail-delete-button"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? 'Suppression...' : "Pas interessant - supprimer"}
            </button>
          </section>

          <section className="listing-detail-card">
            <h2>Estimation du rendement</h2>
            <label className="listing-detail-field">
              Loyer mensuel estime / logement (EUR)
              <input type="number" value={rent} onChange={(e) => setRent(e.target.value)} />
            </label>
            <label className="listing-detail-field">
              Frais d'acquisition estimes (EUR)
              <input
                type="number"
                value={acquisitionCosts}
                onChange={(e) => setAcquisitionCosts(e.target.value)}
              />
            </label>
            <label className="listing-detail-field">
              Budget de renovation (EUR)
              <input
                type="number"
                value={renovationBudget}
                onChange={(e) => setRenovationBudget(e.target.value)}
              />
            </label>

            <p className="listing-detail-yield">
              Rendement brut estime : <strong>{formatPercent(listing.estimatedGrossYield)}</strong>
            </p>

            <label className="listing-detail-field">
              Notes personnelles
              <textarea rows={4} value={notes} onChange={(e) => setNotes(e.target.value)} />
            </label>

            <button
              type="button"
              className="listing-detail-primary-button"
              onClick={handleSave}
              disabled={updateMutation.isPending}
            >
              {updateMutation.isPending ? 'Enregistrement...' : 'Enregistrer'}
            </button>
          </section>
        </aside>
      </div>
    </section>
  )
}
