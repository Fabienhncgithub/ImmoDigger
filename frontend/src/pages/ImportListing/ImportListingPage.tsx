import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../../api/client'
import { useImportListing } from '../../hooks/useImportListing'
import type { ManualListingFields } from '../../types'
import './ImportListingPage.css'

const PROPERTY_TYPE_OPTIONS = [
  { value: 'IncomeBuilding', label: 'Immeuble de rapport' },
  { value: 'House', label: 'Maison' },
  { value: 'Apartment', label: 'Appartement' },
  { value: 'Warehouse', label: 'Entrepôt' },
  { value: 'Office', label: 'Bureau' },
  { value: 'Land', label: 'Terrain' },
  { value: 'Garage', label: 'Garage' },
  { value: 'Other', label: 'Autre' },
]

interface ImportErrorDetails {
  requiresManualFallback?: boolean
  reasons?: string[]
}

function getImportError(error: unknown): ImportErrorDetails | null {
  if (!(error instanceof ApiError) || typeof error.details !== 'object' || error.details === null) {
    return null
  }

  return error.details as ImportErrorDetails
}

export function ImportListingPage() {
  const navigate = useNavigate()
  const importMutation = useImportListing()
  const [url, setUrl] = useState('')
  const [manualMode, setManualMode] = useState(false)
  const [manual, setManual] = useState<ManualListingFields>({ title: '', propertyType: 'IncomeBuilding' })

  const errorDetails = getImportError(importMutation.error)

  function updateManual<K extends keyof ManualListingFields>(key: K, value: ManualListingFields[K]) {
    setManual((current) => ({ ...current, [key]: value }))
  }

  function runImport(manualFallback?: ManualListingFields) {
    importMutation.mutate(
      { url: url.trim(), manualFallback },
      {
        onSuccess: (listing) => navigate(`/listings/${listing.id}`),
        onError: (error) => {
          if (getImportError(error)?.requiresManualFallback) {
            setManualMode(true)
          }
        },
      },
    )
  }

  function handleAutomaticSubmit(event: FormEvent) {
    event.preventDefault()
    importMutation.reset()
    runImport()
  }

  function handleManualSubmit(event: FormEvent) {
    event.preventDefault()
    importMutation.reset()
    runImport({
      ...manual,
      title: manual.title.trim(),
      description: manual.description?.trim() || null,
      address: manual.address?.trim() || null,
      postalCode: manual.postalCode?.trim() || null,
      city: manual.city?.trim() || null,
      imageUrl: manual.imageUrl?.trim() || null,
    })
  }

  return (
    <section className="import-page">
      <h1>Importer une annonce</h1>
      <p className="import-page-intro">
        Colle l’URL d’une annonce que tu consultes. ImmoDigger essaie d’en lire les métadonnées publiques ; si la
        page les bloque, tu peux saisir les informations essentielles manuellement.
      </p>

      <form className="import-card" onSubmit={handleAutomaticSubmit}>
        <label htmlFor="listing-url">URL de l’annonce</label>
        <div className="import-url-row">
          <input
            id="listing-url"
            type="url"
            value={url}
            onChange={(event) => {
              setUrl(event.target.value)
              importMutation.reset()
            }}
            placeholder="https://www.exemple.be/annonce/..."
            required
          />
          <button type="submit" disabled={importMutation.isPending}>
            {importMutation.isPending && !manualMode ? 'Import en cours…' : 'Importer automatiquement'}
          </button>
        </div>
      </form>

      {importMutation.isError && !manualMode && (
        <div className="import-message import-message--error" role="alert">
          {errorDetails?.reasons?.length
            ? errorDetails.reasons.join(' ')
            : 'L’import a échoué. Vérifie l’URL et que l’API est accessible.'}
        </div>
      )}

      {manualMode && (
        <form className="import-card import-manual-form" onSubmit={handleManualSubmit}>
          <div className="import-manual-header">
            <div>
              <h2>Saisie manuelle</h2>
              <p>La page n’expose pas assez de métadonnées. Seul le titre est obligatoire.</p>
            </div>
            <button type="button" className="import-secondary-button" onClick={() => setManualMode(false)}>
              Fermer
            </button>
          </div>

          <div className="import-fields-grid">
            <label className="import-field-wide">
              Titre
              <input
                value={manual.title}
                onChange={(event) => updateManual('title', event.target.value)}
                maxLength={500}
                required
              />
            </label>

            <label>
              Prix demandé (EUR)
              <input
                type="number"
                min="0"
                step="1"
                value={manual.price ?? ''}
                onChange={(event) => updateManual('price', event.target.value ? Number(event.target.value) : null)}
              />
            </label>

            <label>
              Type de bien
              <select
                value={manual.propertyType ?? 'Other'}
                onChange={(event) => updateManual('propertyType', event.target.value)}
              >
                {PROPERTY_TYPE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="import-field-wide">
              Adresse
              <input value={manual.address ?? ''} onChange={(event) => updateManual('address', event.target.value)} />
            </label>

            <label>
              Code postal
              <input
                value={manual.postalCode ?? ''}
                onChange={(event) => updateManual('postalCode', event.target.value)}
                maxLength={20}
              />
            </label>

            <label>
              Ville
              <input value={manual.city ?? ''} onChange={(event) => updateManual('city', event.target.value)} />
            </label>

            <label className="import-field-wide">
              URL de l’image
              <input
                type="url"
                value={manual.imageUrl ?? ''}
                onChange={(event) => updateManual('imageUrl', event.target.value)}
              />
            </label>

            <label className="import-field-wide">
              Description
              <textarea
                rows={5}
                value={manual.description ?? ''}
                onChange={(event) => updateManual('description', event.target.value)}
              />
            </label>
          </div>

          {importMutation.isError && (
            <div className="import-message import-message--error" role="alert">
              {errorDetails?.reasons?.length
                ? errorDetails.reasons.join(' ')
                : 'Impossible d’enregistrer cette annonce. Vérifie les champs puis réessaie.'}
            </div>
          )}

          <button type="submit" disabled={importMutation.isPending}>
            {importMutation.isPending ? 'Enregistrement…' : 'Enregistrer l’annonce'}
          </button>
        </form>
      )}
    </section>
  )
}
