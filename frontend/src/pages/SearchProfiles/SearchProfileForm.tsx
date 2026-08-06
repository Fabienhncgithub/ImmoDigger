import { useState, type FormEvent } from 'react'
import type { SearchProfile, SearchProfileRequest } from '../../types'
import { CommuneMultiSelect } from '../../components/CommuneMultiSelect/CommuneMultiSelect'
import './SearchProfileForm.css'

interface SearchProfileFormProps {
  initial?: SearchProfile
  onSubmit: (request: SearchProfileRequest) => void
  onCancel: () => void
  isSubmitting: boolean
}

const PROPERTY_TYPE_OPTIONS = [
  { value: 'IncomeBuilding', label: 'Immeuble de rapport' },
  { value: 'ApartmentBuilding', label: 'Immeuble a appartements' },
  { value: 'House', label: 'Maison' },
]

export function SearchProfileForm({ initial, onSubmit, onCancel, isSubmitting }: SearchProfileFormProps) {
  const [name, setName] = useState(initial?.name ?? '')
  const [maximumPrice, setMaximumPrice] = useState(initial?.maximumPrice?.toString() ?? '')
  const [minimumGrossYield, setMinimumGrossYield] = useState(initial?.minimumGrossYield?.toString() ?? '')
  const [minimumUnitCount, setMinimumUnitCount] = useState(initial?.minimumUnitCount?.toString() ?? '')
  const [minimumOpportunityScore, setMinimumOpportunityScore] = useState(
    initial?.minimumOpportunityScore?.toString() ?? '',
  )
  const [postalCodes, setPostalCodes] = useState<string[]>(initial?.postalCodes ?? [])
  const [propertyTypes, setPropertyTypes] = useState<string[]>(initial?.propertyTypes ?? [])
  const [requireGarage, setRequireGarage] = useState(initial?.requireGarage ?? false)
  const [includePublicSales, setIncludePublicSales] = useState(initial?.includePublicSales ?? true)
  const [isEnabled, setIsEnabled] = useState(initial?.isEnabled ?? true)

  function togglePropertyType(value: string) {
    setPropertyTypes((current) =>
      current.includes(value) ? current.filter((v) => v !== value) : [...current, value],
    )
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    onSubmit({
      name,
      maximumPrice: maximumPrice ? Number(maximumPrice) : null,
      minimumGrossYield: minimumGrossYield ? Number(minimumGrossYield) : null,
      minimumUnitCount: minimumUnitCount ? Number(minimumUnitCount) : null,
      minimumLivingArea: initial?.minimumLivingArea ?? null,
      requireGarage,
      includePublicSales,
      postalCodes,
      propertyTypes,
      minimumOpportunityScore: minimumOpportunityScore ? Number(minimumOpportunityScore) : null,
      isEnabled,
    })
  }

  return (
    <form className="search-profile-form" onSubmit={handleSubmit}>
      <label>
        Nom
        <input value={name} onChange={(e) => setName(e.target.value)} required />
      </label>

      <div className="search-profile-form-row">
        <label>
          Prix maximum (EUR)
          <input type="number" value={maximumPrice} onChange={(e) => setMaximumPrice(e.target.value)} />
        </label>
        <label>
          Rendement brut minimum (%)
          <input
            type="number"
            step="0.1"
            value={minimumGrossYield}
            onChange={(e) => setMinimumGrossYield(e.target.value)}
          />
        </label>
      </div>

      <div className="search-profile-form-row">
        <label>
          Logements minimum
          <input type="number" value={minimumUnitCount} onChange={(e) => setMinimumUnitCount(e.target.value)} />
        </label>
        <label>
          Score minimum pour notification
          <input
            type="number"
            value={minimumOpportunityScore}
            onChange={(e) => setMinimumOpportunityScore(e.target.value)}
          />
        </label>
      </div>

      <label>
        Communes
        <CommuneMultiSelect selectedPostalCodes={postalCodes} onChange={setPostalCodes} />
      </label>

      <label>
        Types de bien
        <div className="search-profile-form-property-types">
          {PROPERTY_TYPE_OPTIONS.map((option) => (
            <button
              key={option.value}
              type="button"
              className={propertyTypes.includes(option.value) ? 'active' : ''}
              onClick={() => togglePropertyType(option.value)}
            >
              {option.label}
            </button>
          ))}
        </div>
      </label>

      <div className="search-profile-form-checkboxes">
        <label>
          <input type="checkbox" checked={requireGarage} onChange={(e) => setRequireGarage(e.target.checked)} />
          Garage requis
        </label>
        <label>
          <input
            type="checkbox"
            checked={includePublicSales}
            onChange={(e) => setIncludePublicSales(e.target.checked)}
          />
          Inclure les ventes publiques
        </label>
        <label>
          <input type="checkbox" checked={isEnabled} onChange={(e) => setIsEnabled(e.target.checked)} />
          Profil actif
        </label>
      </div>

      <div className="search-profile-form-actions">
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Enregistrement...' : 'Enregistrer'}
        </button>
        <button type="button" className="search-profile-form-cancel" onClick={onCancel}>
          Annuler
        </button>
      </div>
    </form>
  )
}
