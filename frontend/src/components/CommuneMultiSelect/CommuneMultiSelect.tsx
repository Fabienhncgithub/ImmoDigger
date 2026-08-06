import { useState, type KeyboardEvent } from 'react'
import { searchCommunes, BRUSSELS_COMMUNES, type Commune } from '../../data/communes'
import './CommuneMultiSelect.css'

interface CommuneMultiSelectProps {
  selectedPostalCodes: string[]
  onChange: (postalCodes: string[]) => void
}

/**
 * Type-ahead, multi-select commune/postal-code picker, like the ones on the
 * major real-estate portals: type a commune name or a postal code, pick
 * from the suggestions, and the selection becomes a removable chip so
 * several locations can be searched at once.
 */
export function CommuneMultiSelect({ selectedPostalCodes, onChange }: CommuneMultiSelectProps) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)

  const suggestions = searchCommunes(query).filter((c) => !selectedPostalCodes.includes(c.postalCode))

  function selectCommune(commune: Commune) {
    onChange([...selectedPostalCodes, commune.postalCode])
    setQuery('')
    setIsOpen(false)
  }

  function removeCommune(postalCode: string) {
    onChange(selectedPostalCodes.filter((code) => code !== postalCode))
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter' && suggestions.length > 0) {
      event.preventDefault()
      selectCommune(suggestions[0])
    } else if (event.key === 'Escape') {
      setIsOpen(false)
    }
  }

  return (
    <div className="commune-select">
      {selectedPostalCodes.length > 0 && (
        <div className="commune-select-chips">
          {selectedPostalCodes.map((postalCode) => {
            const commune = BRUSSELS_COMMUNES.find((c) => c.postalCode === postalCode)
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
          placeholder="Commune ou code postal..."
          value={query}
          onChange={(e) => {
            setQuery(e.target.value)
            setIsOpen(true)
          }}
          onFocus={() => setIsOpen(true)}
          onBlur={() => setTimeout(() => setIsOpen(false), 150)}
          onKeyDown={handleKeyDown}
        />

        {isOpen && suggestions.length > 0 && (
          <ul className="commune-select-suggestions">
            {suggestions.map((commune) => (
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
