import { useSources, useUpdateSource, useRunCollection } from '../../hooks/useSources'
import { formatDateTime } from '../../utils/format'
import type { Source } from '../../types'
import './SourcesPage.css'

export function SourcesPage() {
  const { data: sources, isLoading } = useSources()
  const updateMutation = useUpdateSource()
  const runMutation = useRunCollection()

  return (
    <section className="sources-page">
      <div className="sources-header">
        <h1>Sources</h1>
        <button
          type="button"
          className="sources-run-button"
          onClick={() => runMutation.mutate()}
          disabled={runMutation.isPending}
        >
          {runMutation.isPending ? 'Lancement...' : 'Lancer une collecte maintenant'}
        </button>
      </div>

      {isLoading && <p className="sources-status">Chargement...</p>}

      <div className="sources-list">
        {sources?.map((source) => (
          <SourceRow
            key={source.id}
            source={source}
            onToggle={(isEnabled) =>
              updateMutation.mutate({
                id: source.id,
                request: { isEnabled, pollingIntervalMinutes: source.pollingIntervalMinutes },
              })
            }
          />
        ))}
      </div>
    </section>
  )
}

const COLLECTION_METHOD_LABELS: Record<Source['collectionMethod'], string> = {
  Api: 'API',
  Rss: 'RSS',
  PublicFeed: 'Flux public',
  Html: 'Page HTML',
  Email: 'Emails d’alerte',
  Manual: 'Import manuel',
  Disabled: 'Aucune',
}

function SourceRow({ source, onToggle }: { source: Source; onToggle: (isEnabled: boolean) => void }) {
  return (
    <div className="source-row">
      <div className="source-row-main">
        <div>
          <h2>{source.name}</h2>
          <p className="source-row-url">{source.baseUrl}</p>
        </div>
        <div className="source-row-status">
          <span className="source-row-method">
            <span
              className={`source-row-method-dot source-row-method-dot--${source.allowed ? 'good' : 'critical'}`}
              aria-hidden="true"
            />
            {COLLECTION_METHOD_LABELS[source.collectionMethod]}
            {!source.allowed && ' (non autorisee)'}
          </span>
          <label className="source-row-toggle">
            <input type="checkbox" checked={source.isEnabled} onChange={(e) => onToggle(e.target.checked)} />
            {source.isEnabled ? 'Activee' : 'Desactivee'}
          </label>
        </div>
      </div>

      {source.isEnabled && !source.allowed && (
        <p className="source-row-blocked-warning">
          Activee mais ne collectera jamais rien : cette source n'est pas autorisee (scraping direct interdit ou
          bloque techniquement). Voir la note ci-dessous.
        </p>
      )}

      <dl className="source-row-facts">
        <div>
          <dt>Annonces collectees</dt>
          <dd>{source.listingCount}</dd>
        </div>
        <div>
          <dt>Derniere execution reussie</dt>
          <dd>{formatDateTime(source.lastSuccessfulRunAt)}</dd>
        </div>
        <div>
          <dt>Dernier echec</dt>
          <dd>{formatDateTime(source.lastFailedRunAt)}</dd>
        </div>
        <div>
          <dt>Frequence</dt>
          <dd>{source.pollingIntervalMinutes} min</dd>
        </div>
      </dl>

      {source.notes && <p className="source-row-notes">{source.notes}</p>}
      {source.lastError && <p className="source-row-error">{source.lastError}</p>}
    </div>
  )
}
