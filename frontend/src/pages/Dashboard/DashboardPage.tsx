import { useDashboardSummary } from '../../hooks/useDashboard'
import { StatTile } from '../../components/StatTile/StatTile'
import { PropertyCard } from '../../components/PropertyCard/PropertyCard'
import { formatPrice, formatScore } from '../../utils/format'
import './DashboardPage.css'

export function DashboardPage() {
  const { data: summary, isLoading, isError } = useDashboardSummary()

  return (
    <section className="dashboard-page">
      <h1>Tableau de bord</h1>

      {isLoading && <p className="dashboard-placeholder">Chargement...</p>}

      {isError && (
        <p className="dashboard-placeholder">
          Impossible de charger le tableau de bord. Verifiez que l'API et PostgreSQL tournent.
        </p>
      )}

      {summary && (
        <>
          <div className="dashboard-stats">
            <StatTile label="Nouvelles annonces aujourd'hui" value={String(summary.newListingsToday)} />
            <StatTile label="Annonces actives" value={String(summary.activeListingsCount)} />
            <StatTile label="Prix moyen" value={formatPrice(summary.averagePrice)} />
            <StatTile label="Score moyen" value={formatScore(summary.averageScore)} />
            <StatTile
              label="Opportunites fortes"
              value={String(summary.strongOpportunitiesCount)}
              tone={summary.strongOpportunitiesCount > 0 ? 'good' : 'neutral'}
            />
            <StatTile
              label="Biens a risque eleve"
              value={String(summary.highRiskCount)}
              tone={summary.highRiskCount > 0 ? 'critical' : 'neutral'}
            />
          </div>

          <h2>Dernieres annonces detectees</h2>
          {summary.recentListings.length === 0 ? (
            <p className="dashboard-placeholder">
              Aucune annonce pour le moment. Active le mode demonstration ou lance une collecte.
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
