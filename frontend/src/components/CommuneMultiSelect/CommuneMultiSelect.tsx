import { useState, type KeyboardEvent } from 'react'
import {
  searchCommunes,
  searchRegions,
  BELGIAN_COMMUNES,
  BELGIAN_REGIONS,
  type Commune,
  type CommuneRegion,
} from '../../data/communes'
import './CommuneMultiSelect.css'

interface CommuneMultiSelectProps {
  selectedPostalCodes: string[]
  onChange: (postalCodes: string[]) => void
}

/**
 * Type-ahead, multi-select commune/postal-code picker, like the ones on the
 * major real-estate portals: type a commune name or a postal code, pick
 * from the suggestions, and the selection becomes a removable chip so
 * several locations can be searched at once. Typing a region name (e.g.
 * "Bruxelles") also offers "toute la region" as a one-click pick for every
 * commune in it - still just OR-matched postal codes underneath, so no
 * change on the API side.
 */
export function CommuneMultiSelect({ selectedPostalCodes, onChange }: CommuneMultiSelectProps) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)

  const communeSuggestions = searchCommunes(query).filter((c) => !selectedPostalCodes.includes(c.postalCode))
  const regionSuggestions = searchRegions(query).filter(
    (region) => !region.postalCodes.every((pc) => selectedPostalCodes.includes(pc)),
  )

  // A region whose every commune is currently selected collapses into a
  // single chip instead of cluttering the bar with 19+ individual ones.
  const fullyCoveredRegions = communeRegionsFullyCoveredBy(selectedPostalCodes)
  const regionCoveredCodes = new Set(fullyCoveredRegions.flatMap((r) => r.postalCodes))
  const individualPostalCodes = selectedPostalCodes.filter((pc) => !regionCoveredCodes.has(pc))

  function selectCommune(commune: Commune) {
    onChange([...selectedPostalCodes, commune.postalCode])
    setQuery('')
    setIsOpen(false)
  }

  function selectRegion(region: CommuneRegion) {
    onChange([...new Set([...selectedPostalCodes, ...region.postalCodes])])
    setQuery('')
    setIsOpen(false)
  }

  function removeCommune(postalCode: string) {
    onChange(selectedPostalCodes.filter((code) => code !== postalCode))
  }

  function removeRegion(region: CommuneRegion) {
    onChange(selectedPostalCodes.filter((code) => !region.postalCodes.includes(code)))
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      if (regionSuggestions.length > 0) {
        event.preventDefault()
        selectRegion(regionSuggestions[0])
      } else if (communeSuggestions.length > 0) {
        event.preventDefault()
        selectCommune(communeSuggestions[0])
      }
    } else if (event.key === 'Escape') {
      setIsOpen(false)
    }
  }

  return (
    <div className="commune-select">
      {(fullyCoveredRegions.length > 0 || individualPostalCodes.length > 0) && (
        <div className="commune-select-chips">
          {fullyCoveredRegions.map((region) => (
            <span key={region.name} className="commune-select-chip commune-select-chip-region">
              {region.name} ({region.postalCodes.length} codes postaux)
              <button type="button" onClick={() => removeRegion(region)} aria-label={`Retirer ${region.name}`}>
                ×
              </button>
            </span>
          ))}
          {individualPostalCodes.map((postalCode) => {
            const commune = BELGIAN_COMMUNES.find((c) => c.postalCode === postalCode)
            return (
              <span key={postalCode} className="commune-select-chip">
                {commune ? `${commune.name} (${postalCode})` : postalCode}
                <button type="button" onClick={() => removeCommune(postalCode)} aria-label={`Retirer ${postalCode}`}>
                  ×
                </button>
              </span>
            )
          })}
        </div>
      )}

      <div className="commune-select-input-wrapper">
        <input
          type="text"
          placeholder="Commune, code postal ou region..."
          value={query}
          onChange={(e) => {
            setQuery(e.target.value)
            setIsOpen(true)
          }}
          onFocus={() => setIsOpen(true)}
          onBlur={() => setTimeout(() => setIsOpen(false), 150)}
          onKeyDown={handleKeyDown}
        />

        {isOpen && (regionSuggestions.length > 0 || communeSuggestions.length > 0) && (
          <ul className="commune-select-suggestions">
            {regionSuggestions.map((region) => (
              <li key={region.name}>
                <button
                  type="button"
                  className="commune-select-suggestion-region"
                  onMouseDown={() => selectRegion(region)}
                >
                  <span>Toute la region : {region.name}</span>
                  <span className="commune-select-suggestion-code">{region.postalCodes.length} codes postaux</span>
                </button>
              </li>
            ))}
            {communeSuggestions.map((commune) => (
              <li key={commune.postalCode}>
                <button type="button" onMouseDown={() => selectCommune(commune)}>
                  <span>{commune.name}</span>
                  <span className="commune-select-suggestion-code">{commune.postalCode}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}

function communeRegionsFullyCoveredBy(selectedPostalCodes: string[]): CommuneRegion[] {
  return BELGIAN_REGIONS.filter((region) => region.postalCodes.every((pc) => selectedPostalCodes.includes(pc)))
}
