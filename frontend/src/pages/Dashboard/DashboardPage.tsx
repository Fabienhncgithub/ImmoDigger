import './DashboardPage.css'

/**
 * Placeholder dashboard. The real summary widgets (new listings today,
 * average price, opportunity score, ...) are added in a later commit
 * once the backend dashboard endpoint exists.
 */
export function DashboardPage() {
  return (
    <section className="dashboard-page">
      <h1>Tableau de bord</h1>
      <p className="dashboard-placeholder">
        ImmoDigger est en cours d'initialisation. Les indicateurs
        apparaîtront ici une fois la collecte et l'analyse des annonces mises
        en place.
      </p>
    </section>
  )
}
