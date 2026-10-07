import { useEffect, useState } from 'react'
import {
  useCollectionStatus,
  useEmailImportStatus,
  useSources,
  useUpdateSource,
  useRunCollection,
  useTestEmailImport,
} from '../../hooks/useSources'
import { formatDateTime } from '../../utils/format'
import type { Source, UpdateSourceRequest } from '../../types'
import './SourcesPage.css'

export function SourcesPage() {
  const { data: sources, isLoading, isError } = useSources()
  const { data: collectionStatus } = useCollectionStatus()
  const { data: emailImportStatus } = useEmailImportStatus()
  const updateMutation = useUpdateSource()
  const runMutation = useRunCollection()
  const testEmailMutation = useTestEmailImport()

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

      <p className="sources-last-run">
        Dernière collecte terminée : {formatDateTime(collectionStatus?.lastRunAt ?? null)}
      </p>

      {emailImportStatus && (
        <aside
          className={`sources-email-readiness ${
            emailImportStatus.isConfigured ? 'sources-email-readiness--ready' : 'sources-email-readiness--missing'
          }`}
        >
          <strong>
            {emailImportStatus.isConfigured
              ? 'Boîte email connectée'
              : 'Immoweb et autres portails : configuration IMAP requise'}
          </strong>
          <p>
            {emailImportStatus.isConfigured
              ? `${emailImportStatus.supportedSources.length} portails pris en charge. Les nouvelles alertes sont lues automatiquement dans ${emailImportStatus.folder}.`
              : ` À renseigner dans .env : ${emailImportStatus.configurationIssues.join(', ')}.`}
          </p>
          {emailImportStatus.isConfigured && (
            <details className="sources-email-details">
              <summary>Voir les portails pris en charge</summary>
              <p>{emailImportStatus.supportedSources.join(' · ')}</p>
            </details>
          )}
          {emailImportStatus.isConfigured && (
            <div className="sources-email-test">
              <button
                type="button"
                onClick={() => testEmailMutation.mutate()}
                disabled={testEmailMutation.isPending}
              >
                {testEmailMutation.isPending ? 'Test en cours…' : 'Tester la connexion IMAP'}
              </button>
              {testEmailMutation.data && <span>{testEmailMutation.data.message}</span>}
              {testEmailMutation.isError && <span>Connexion impossible. Vérifiez les réglages et les logs.</span>}
            </div>
          )}
        </aside>
      )}

      {isLoading && <p className="sources-status">Chargement...</p>}
      {isError && <p className="sources-status sources-status--error">Impossible de charger les sources.</p>}
      {runMutation.isSuccess && (
        <p className="sources-status sources-status--success">
          Collecte lancée. Les résultats et statuts se mettent à jour automatiquement.
        </p>
      )}
      {runMutation.isError && (
        <p className="sources-status sources-status--error">Impossible de lancer la collecte.</p>
      )}
      {updateMutation.isError && (
        <p className="sources-status sources-status--error">Impossible d’enregistrer la source.</p>
      )}

      <div className="sources-list">
        {sources?.map((source) => (
          <SourceRow
            key={source.id}
            source={source}
            isSaving={updateMutation.isPending}
            onUpdate={(request) =>
              updateMutation.mutate({
                id: source.id,
                request,
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

function SourceRow({
  source,
  isSaving,
  onUpdate,
}: {
  source: Source
  isSaving: boolean
  onUpdate: (request: UpdateSourceRequest) => void
}) {
  const [pollingInterval, setPollingInterval] = useState(String(source.pollingIntervalMinutes))
  const isExternalAlertSource = source.collectionMethod === 'Email' && !source.allowed

  useEffect(() => setPollingInterval(String(source.pollingIntervalMinutes)), [source.pollingIntervalMinutes])

  function savePollingInterval() {
    const value = Number(pollingInterval)
    if (!Number.isInteger(value) || value < 1) {
      setPollingInterval(String(source.pollingIntervalMinutes))
      return
    }

    onUpdate({ isEnabled: source.isEnabled, pollingIntervalMinutes: value })
  }

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
              className={`source-row-method-dot source-row-method-dot--${
                source.allowed || isExternalAlertSource ? 'good' : 'critical'
              }`}
              aria-hidden="true"
            />
            {COLLECTION_METHOD_LABELS[source.collectionMethod]}
            {isExternalAlertSource ? ' (via EmailImport)' : !source.allowed && ' (non autorisée)'}
          </span>
          <label className="source-row-toggle">
            {isExternalAlertSource ? (
              'Via EmailImport'
            ) : (
              <>
                <input
                  type="checkbox"
                  checked={source.isEnabled}
                  disabled={isSaving}
                  onChange={(e) =>
                    onUpdate({ isEnabled: e.target.checked, pollingIntervalMinutes: source.pollingIntervalMinutes })
                  }
                />
                {source.isEnabled ? 'Activée' : 'Désactivée'}
              </>
            )}
          </label>
        </div>
      </div>

      {source.isEnabled && !source.allowed && !isExternalAlertSource && (
        <p className="source-row-blocked-warning">
          Activee mais ne collectera jamais rien : cette source n'est pas autorisee (scraping direct interdit ou
          bloque techniquement). Voir la note ci-dessous.
        </p>
      )}

      <dl className="source-row-facts">
        <div>
          <dt>Annonces réelles</dt>
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
          <dd className="source-row-polling">
            <input
              type="number"
              min="1"
              value={pollingInterval}
              aria-label={`Fréquence de collecte de ${source.name} en minutes`}
              onChange={(event) => setPollingInterval(event.target.value)}
              onBlur={savePollingInterval}
              onKeyDown={(event) => {
                if (event.key === 'Enter') event.currentTarget.blur()
              }}
              disabled={isSaving || isExternalAlertSource}
            />
            min
          </dd>
        </div>
      </dl>

      {source.notes && (
        <details className="source-row-technical">
          <summary>Détails techniques</summary>
          <p className="source-row-notes">{source.notes}</p>
        </details>
      )}
      {source.lastError && <p className="source-row-error">{source.lastError}</p>}
    </div>
  )
}
