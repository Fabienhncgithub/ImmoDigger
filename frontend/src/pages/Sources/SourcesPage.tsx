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

function SourceRow({ source, onToggle }: { source: Source; onToggle: (isEnabled: boolean) => void }) {
  return (
    <div className="source-row">
      <div className="source-row-main">
        <div>
          <h2>{source.name}</h2>
          <p className="source-row-url">{source.baseUrl}</p>
        </div>
        <label className="source-row-toggle">
          <input type="checkbox" checked={source.isEnabled} onChange={(e) => onToggle(e.target.checked)} />
          {source.isEnabled ? 'Activee' : 'Desactivee'}
        </label>
      </div>

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

      {source.lastError && <p className="source-row-error">{source.lastError}</p>}
    </div>
  )
}
