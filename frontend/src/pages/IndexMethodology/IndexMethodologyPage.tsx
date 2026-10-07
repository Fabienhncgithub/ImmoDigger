import { Link } from 'react-router-dom'
import { useIndexMethodology } from '../../hooks/useListings'
import './IndexMethodologyPage.css'

/**
 * Explains the ImmoDigger index. Every figure on this page comes from the
 * API, which builds it from the scales the analysis actually scores with.
 */
export function IndexMethodologyPage() {
  const { data: methodology, isLoading, isError } = useIndexMethodology()

  return (
    <section className="index-page">
      <header className="index-page-header">
        <h1>Comment l’indice est calculé</h1>
        <p>
          L’indice ImmoDigger compare les annonces entre elles sur six critères. C’est un outil de tri,
          pas une estimation de valeur ni un conseil d’investissement.
        </p>
      </header>

      {isLoading && <p className="index-page-status">Chargement...</p>}
      {isError && <p className="index-page-status">Impossible de charger le barème.</p>}

      {methodology && (
        <>
          <div className="index-page-card">
            <h2>Le principe</h2>
            <ol className="index-page-steps">
              <li>
                Chaque critère rapporte des points, pour un total de <b>{methodology.totalPoints} points</b>.
              </li>
              <li>
                Un critère dont la donnée manque est <b>ignoré</b> : il ne compte ni pour, ni contre le bien.
              </li>
              <li>
                L’indice est la part des points obtenus sur les points <b>disponibles</b>, ramenée sur 100.
              </li>
              <li>
                Sous <b>{methodology.minimumAvailablePoints} points disponibles</b>, il y a trop peu de données :
                aucun indice n’est affiché.
              </li>
              <li>
                À partir de <b>{methodology.strongOpportunityThreshold}/100</b>, le bien compte parmi les
                « opportunités fortes » du tableau de bord.
              </li>
            </ol>
            <p className="index-page-example">
              <b>Exemple.</b> Un bien dont on connaît seulement le prix au m² (20 points sur 25) et la commune
              (15 sur 15) obtient 35 points sur 40 disponibles, soit 88/100.
            </p>
          </div>

          <div className="index-page-card index-page-card--warning">
            <h2>À garder en tête</h2>
            <ul>
              <li>
                <b>Regardez le nombre de critères.</b> Un 88/100 calculé sur 2 critères ne vaut pas un 70/100
                calculé sur 5. Le nombre de critères évalués est affiché à côté de chaque indice.
              </li>
              <li>
                Les alertes e-mail d’Immoweb et d’Immovlan ne donnent ni les loyers, ni le nombre de logements,
                ni le PEB : pour ces annonces l’indice repose le plus souvent sur le prix au m² et la commune.
              </li>
              <li>
                Le rendement n’entre dans l’indice que si vous saisissez un loyer sur la fiche du bien.
              </li>
              <li>
                Les seuils de prix au m² sont les mêmes pour tous les types de biens et toutes les communes.
              </li>
              <li>
                En vente publique, l’enchère en cours est souvent bien plus basse que le prix final : le prix
                au m² est alors flatteur.
              </li>
            </ul>
          </div>

          <h2 className="index-page-section-title">Les six critères</h2>
          <div className="index-page-criteria">
            {methodology.criteria.map((criterion) => (
              <article key={criterion.key} className="index-page-card index-page-criterion">
                <div className="index-page-criterion-heading">
                  <h3>{criterion.label}</h3>
                  <span>{criterion.maxPoints} points</span>
                </div>
                <p>{criterion.basis}</p>
                <p className="index-page-criterion-when">
                  <b>Pris en compte si :</b> {criterion.countedWhen}
                </p>
                <table>
                  <tbody>
                    {criterion.steps.map((step) => (
                      <tr key={step.condition}>
                        <td>{step.condition}</td>
                        <td>{step.points} pts</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </article>
            ))}
          </div>

          <p className="index-page-footer">
            Sur la fiche d’un bien, le bouton « Calculer l’indice » affiche le détail des points obtenus,
            critère par critère. <Link to="/listings?sortBy=score&minimumScore=0">Voir les biens classés par indice</Link>
          </p>
        </>
      )}
    </section>
  )
}
