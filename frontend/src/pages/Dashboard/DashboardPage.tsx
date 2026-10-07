import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useDashboardSummary } from '../../hooks/useDashboard'
import { StatTile } from '../../components/StatTile/StatTile'
import { PropertyCard } from '../../components/PropertyCard/PropertyCard'
import { AppIcon } from '../../components/Icon/AppIcon'
import { formatPrice, formatScore } from '../../utils/format'
import './DashboardPage.css'

// Same threshold as DashboardController.StrongOpportunityThreshold.
const STRONG_OPPORTUNITY_SCORE = 70

// Each tile opens the listings behind its figure, with the same scope the
// KPI is computed on (real, active listings).
const REAL_ACTIVE = 'isActive=true&excludeDemo=true'
const TILE_LINKS = {
  active: `/listings?${REAL_ACTIVE}`,
  newToday: `/listings?${REAL_ACTIVE}&weeks=0`,
  price: `/listings?${REAL_ACTIVE}&sortBy=price`,
  score: `/listings?${REAL_ACTIVE}&minimumScore=0&sortBy=score`,
  strong: `/listings?${REAL_ACTIVE}&minimumScore=${STRONG_OPPORTUNITY_SCORE}&sortBy=score`,
  highRisk: `/listings?${REAL_ACTIVE}&riskLevel=High`,
}

export function DashboardPage() {
  const { data: summary, isLoading, isError } = useDashboardSummary()
  const navigate = useNavigate()
  const [searchText, setSearchText] = useState('')

  function handleSearch(event: FormEvent) {
    event.preventDefault()
    const query = searchText.trim()
    navigate(query ? `/listings?searchText=${encodeURIComponent(query)}` : '/listings')
  }

  return (
    <section className="dashboard-page">
      <header className="dashboard-hero">
        <p className="dashboard-eyebrow">
          <span className="dashboard-live-dot" aria-hidden="true" />
          Radar immobilier en direct
        </p>
        <h1>Repérez la valeur avant les autres</h1>
        <p className="dashboard-intro">
          Les annonces, les points de vigilance et le potentiel d’investissement réunis dans une
          seule vue claire.
        </p>
        <form className="dashboard-search" role="search" onSubmit={handleSearch}>
          <AppIcon name="search" />
          <input
            type="search"
            placeholder="Commune, adresse, mot-clé…"
            aria-label="Rechercher une annonce"
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
          />
          <button type="submit" className="dashboard-primary-action">
            Rechercher
          </button>
        </form>
        <div className="dashboard-hero-actions">
          <Link to="/listings" className="dashboard-secondary-action">
            Toutes les annonces
            <AppIcon name="arrow" />
          </Link>
          <Link to="/import" className="dashboard-secondary-action">
            Importer un bien
          </Link>
        </div>
      </header>

      {isLoading && <p className="dashboard-placeholder">Chargement...</p>}

      {isError && (
        <p className="dashboard-placeholder">
          Impossible de charger le tableau de bord. Verifiez que l'API et PostgreSQL tournent.
        </p>
      )}

      {summary && (
        <>
          {summary.demoActiveListingsCount > 0 && (
            <div className="dashboard-data-notice">
              <span className="dashboard-data-notice-icon">
                <AppIcon name="database" />
              </span>
              <div>
                <strong>Les démonstrations sont exclues des calculs</strong>
                <p>
                  Les indicateurs portent uniquement sur{' '}
                  <b>{summary.realActiveListingsCount} biens réels</b>. Les{' '}
                  <b>{summary.demoActiveListingsCount} fiches fictives</b> restent visibles pour tester
                  l’interface et portent le badge « Démo ».
                </p>
              </div>
            </div>
          )}

          <div className="dashboard-section-heading dashboard-section-heading--overview">
            <div>
              <p className="dashboard-section-kicker">Vue d’ensemble</p>
              <h2>Le marché en un coup d’œil</h2>
            </div>
            <p>Mis à jour automatiquement depuis vos sources actives.</p>
          </div>

          <div className="dashboard-stats">
            <StatTile
              label="Biens réels actifs"
              value={String(summary.realActiveListingsCount)}
              icon="building"
              hint={`${summary.activeListingsCount} fiches au total · ${summary.demoActiveListingsCount} démo exclues`}
              featured
              to={TILE_LINKS.active}
            />
            <StatTile
              label="Nouvelles aujourd’hui"
              value={String(summary.newListingsToday)}
              icon="spark"
              hint="Première détection aujourd’hui"
              to={TILE_LINKS.newToday}
            />
            <StatTile
              label="Prix moyen"
              value={formatPrice(summary.averagePrice)}
              icon="euro"
              hint={summary.pricedActiveListingsCount > 0
                ? `Sur ${summary.pricedActiveListingsCount} bien${summary.pricedActiveListingsCount > 1 ? 's' : ''} réel${summary.pricedActiveListingsCount > 1 ? 's' : ''} avec un prix`
                : 'Aucun prix fourni sur les biens réels'}
              to={TILE_LINKS.price}
            />
            <StatTile
              label="Indice moyen"
              value={formatScore(summary.averageScore)}
              icon="chart"
              hint={summary.scoredActiveListingsCount > 0
                ? `Sur ${summary.scoredActiveListingsCount} bien${summary.scoredActiveListingsCount > 1 ? 's' : ''} assez documenté${summary.scoredActiveListingsCount > 1 ? 's' : ''}`
                : 'Aucun bien ne contient encore assez de données'}
              to={TILE_LINKS.score}
            />
            <StatTile
              label="Opportunités fortes"
              value={String(summary.strongOpportunitiesCount)}
              tone={summary.strongOpportunitiesCount > 0 ? 'good' : 'neutral'}
              icon="home"
              hint={`Biens assez documentés avec un indice ≥ ${STRONG_OPPORTUNITY_SCORE}/100`}
              to={TILE_LINKS.strong}
            />
            <StatTile
              label="Alertes majeures"
              value={String(summary.highRiskCount)}
              tone={summary.highRiskCount > 0 ? 'critical' : 'neutral'}
              icon="shield"
              hint="Anomalies explicites qui nécessitent une vérification prioritaire"
              to={TILE_LINKS.highRisk}
            />
          </div>

          <details className="dashboard-metric-guide">
            <summary>
              <span>Comment lire ces chiffres ?</span>
              <span className="dashboard-metric-guide-toggle" aria-hidden="true">+</span>
            </summary>
            <div className="dashboard-metric-guide-grid">
              <div>
                <strong>Biens suivis</strong>
                <p>Annonces réelles marquées actives, toutes sources confondues. Les démos sont exclues.</p>
              </div>
              <div>
                <strong>Prix moyen</strong>
                <p>Moyenne des prix demandés connus. Les biens « prix sur demande » sont exclus.</p>
              </div>
              <div>
                <strong>Indice ImmoDigger /100</strong>
                <p>
                  Indice comparatif calculé sur le prix/m², rendement, logements, lieu, énergie et points de
                  vigilance. Ce n’est pas une estimation de valeur.{' '}
                  <Link to="/indice">Voir le barème complet</Link>
                </p>
              </div>
              <div>
                <strong>Opportunité forte</strong>
                <p>Bien assez documenté dont l’indice atteint au moins 70/100.</p>
              </div>
              <div>
                <strong>Alerte majeure</strong>
                <p>Anomalie explicite détectée, comme des logements non reconnus ou une non-conformité. Une information simplement absente reste « à vérifier ».</p>
              </div>
              <div>
                <strong>Valeur inconnue</strong>
                <p>Une donnée absente est exclue du calcul, jamais comptée comme zéro. Sous 40 % de données disponibles, aucun indice n’est affiché.</p>
              </div>
            </div>
          </details>

          <div className="dashboard-section-heading dashboard-section-heading--listings">
            <div>
              <p className="dashboard-section-kicker">Sélection récente</p>
              <h2>Derniers biens détectés</h2>
            </div>
            <Link to="/listings" className="dashboard-view-all">
              Voir toutes les annonces
              <AppIcon name="arrow" />
            </Link>
          </div>

          {summary.recentListings.length === 0 ? (
            <p className="dashboard-placeholder">
              Aucune annonce pour le moment. Activez une source ou lancez une collecte.
            </p>
          ) : (
            <div className="dashboard-recent-grid">
              {summary.recentListings.map((listing) => (
                <PropertyCard key={listing.id} listing={listing} />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  )
}
