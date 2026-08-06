import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  useAnalyzeListing,
  useListing,
  useMarkReviewed,
  usePriceHistory,
  useUpdateListing,
} from '../../hooks/useListings'
import { ScoreBadge } from '../../components/Badge/ScoreBadge'
import { RiskBadge } from '../../components/Badge/RiskBadge'
import { PebBadge } from '../../components/Badge/PebBadge'
import {
  formatArea,
  formatDate,
  formatDateTime,
  formatPercent,
  formatPrice,
  formatPropertyType,
  formatSaleType,
} from '../../utils/format'
import type { OpportunityScoreBreakdown } from '../../types'
import './ListingDetailPage.css'

export function ListingDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { data: listing, isLoading, isError } = useListing(id)
  const { data: priceHistory } = usePriceHistory(id)
  const analyzeMutation = useAnalyzeListing(id ?? '')
  const updateMutation = useUpdateListing(id ?? '')
  const markReviewedMutation = useMarkReviewed(id ?? '')

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

  return (
    <section className="listing-detail-page">
      <Link to="/listings" className="listing-detail-back">
        ← Retour aux annonces
      </Link>

      <header className="listing-detail-header">
        <div>
          <span className="listing-detail-source">{listing.source}</span>
          <h1>{listing.title}</h1>
          <p className="listing-detail-address">
            {listing.address ? `${listing.address}, ` : ''}
            {listing.postalCode} {listing.city}
          </p>
        </div>
        <div className="listing-detail-badges">
          <ScoreBadge score={listing.opportunityScore} />
          <RiskBadge riskLevel={listing.riskLevel} />
          <PebBadge pebRating={listing.pebRating} />
        </div>
      </header>

      <div className="listing-detail-grid">
        <div className="listing-detail-main">
          <section className="listing-detail-card">
            <h2>Informations</h2>
            <dl className="listing-detail-facts">
              <div>
                <dt>Prix demande</dt>
                <dd>{formatPrice(listing.askingPrice)}</dd>
              </div>
              <div>
                <dt>Enchere actuelle</dt>
                <dd>{formatPrice(listing.currentBid)}</dd>
              </div>
              <div>
                <dt>Type de vente</dt>
                <dd>{formatSaleType(listing.saleType)}</dd>
              </div>
              <div>
                <dt>Type de bien</dt>
                <dd>{formatPropertyType(listing.propertyType)}</dd>
              </div>
              <div>
                <dt>Logements (observes / officiels)</dt>
                <dd>
                  {listing.observedUnitCount ?? '—'} / {listing.officialUnitCount ?? '—'}
                </dd>
              </div>
              <div>
                <dt>Chambres / Salles de bain</dt>
                <dd>
                  {listing.bedroomCount ?? '—'} / {listing.bathroomCount ?? '—'}
                </dd>
              </div>
              <div>
                <dt>Surface habitable</dt>
                <dd>{formatArea(listing.livingArea)}</dd>
              </div>
              <div>
                <dt>Surface du terrain</dt>
                <dd>{formatArea(listing.landArea)}</dd>
              </div>
              <div>
                <dt>Installation electrique</dt>
                <dd>
                  {listing.electricalInstallationCompliant === null
                    ? '—'
                    : listing.electricalInstallationCompliant
                      ? 'Conforme'
                      : 'Non conforme'}
                </dd>
              </div>
              <div>
                <dt>Occupation</dt>
                <dd>{listing.isOccupied === null ? '—' : listing.isOccupied ? 'Occupe' : 'Libre'}</dd>
              </div>
              <div>
                <dt>Garage / Terrasse / Jardin</dt>
                <dd>
                  {[
                    listing.hasGarage ? 'Garage' : null,
                    listing.hasTerrace ? 'Terrasse' : null,
                    listing.hasGarden ? 'Jardin' : null,
                  ]
                    .filter(Boolean)
                    .join(', ') || '—'}
                </dd>
              </div>
              <div>
                <dt>Premiere detection</dt>
                <dd>{formatDate(listing.firstSeenAt)}</dd>
              </div>
            </dl>
          </section>

          <section className="listing-detail-card">
            <h2>Description</h2>
            <p className="listing-detail-description">{listing.description || 'Aucune description disponible.'}</p>
            <a href={listing.url} target="_blank" rel="noreferrer" className="listing-detail-link">
              Voir l'annonce originale ↗
            </a>
          </section>

          <section className="listing-detail-card">
            <h2>Risques</h2>
            <p className="listing-detail-description">
              {listing.riskSummary || "Aucune analyse de risque n'a encore ete calculee."}
            </p>
          </section>

          {breakdown && (
            <section className="listing-detail-card">
              <h2>Decomposition du score</h2>
              <ul className="listing-detail-score-breakdown">
                <li>
                  <span>Prix / m²</span>
                  <span>{breakdown.pricePerSquareMeterScore} / 25</span>
                </li>
                <li>
                  <span>Rendement</span>
                  <span>{breakdown.grossYieldScore} / 25</span>
                </li>
                <li>
                  <span>Logements</span>
                  <span>{breakdown.unitCountScore} / 15</span>
                </li>
                <li>
                  <span>Localisation</span>
                  <span>{breakdown.locationScore} / 15</span>
                </li>
                <li>
                  <span>Energie</span>
                  <span>{breakdown.energyScore} / 10</span>
                </li>
                <li>
                  <span>Risque</span>
                  <span>{breakdown.riskScore} / 10</span>
                </li>
                <li className="listing-detail-score-total">
                  <span>Total</span>
                  <span>{breakdown.totalScore} / 100</span>
                </li>
              </ul>
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

          <section className="listing-detail-card">
            <h2>Historique des prix</h2>
            {priceHistory && priceHistory.length > 0 ? (
              <table className="listing-detail-price-history">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Prix</th>
                  </tr>
                </thead>
                <tbody>
                  {priceHistory.map((entry) => (
                    <tr key={entry.id}>
                      <td>{formatDate(entry.recordedAt)}</td>
                      <td>{formatPrice(entry.price)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <p className="listing-detail-description">Aucun historique de prix disponible.</p>
            )}
          </section>
        </div>

        <aside className="listing-detail-sidebar">
          <section className="listing-detail-card">
            <h2>Actions</h2>
            <button type="button" className="listing-detail-primary-button" onClick={handleAnalyze}>
              {analyzeMutation.isPending ? 'Analyse...' : 'Analyser'}
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
