import { useEffect, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { useListings } from '../../hooks/useListings'
import { useSources } from '../../hooks/useSources'
import { PropertyCard } from '../../components/PropertyCard/PropertyCard'
import { ListingsTable } from '../../components/ListingsTable/ListingsTable'
import { Pagination } from '../../components/Pagination/Pagination'
import { CommuneMultiSelect } from '../../components/CommuneMultiSelect/CommuneMultiSelect'
import type { ListingQueryParams } from '../../types'
import './ListingsPage.css'

type ViewMode = 'grid' | 'table'

const PAGE_SIZE = 12

const emptyFilters: ListingQueryParams = {
  sortBy: 'firstSeenAt',
  sortDescending: true,
}

const PROPERTY_TYPE_OPTIONS = [
  { value: 'IncomeBuilding', label: 'Immeuble de rapport' },
  { value: 'ApartmentBuilding', label: 'Immeuble a appartements' },
  { value: 'House', label: 'Maison' },
  { value: 'Warehouse', label: 'Entrepot' },
]

/** "depuis N semaines" options for filters.firstSeenFrom - computed at pick time, not stored as a fixed date. */
const PERIOD_OPTIONS = [
  { label: 'Periode (toutes)', weeks: null },
  { label: '1 semaine', weeks: 1 },
  { label: '2 semaines', weeks: 2 },
  { label: '4 semaines', weeks: 4 },
  { label: '3 mois', weeks: 13 },
]

function weeksAgoIso(weeks: number): string {
  const date = new Date()
  date.setDate(date.getDate() - weeks * 7)
  return date.toISOString()
}

export function ListingsPage() {
  // A search profile's "voir les annonces correspondantes" link navigates
  // here with its filters in router state (see searchProfileToListingFilters);
  // the lazy initializer only runs once, so a filter changed afterwards
  // isn't overwritten by a stale location.state on re-render.
  const location = useLocation()
  const [filters, setFilters] = useState<ListingQueryParams>(
    () => (location.state as { filters?: ListingQueryParams } | null)?.filters ?? emptyFilters,
  )
  const [searchDraft, setSearchDraft] = useState('')
  const [page, setPage] = useState(1)
  const [viewMode, setViewMode] = useState<ViewMode>('grid')
  // Tracked separately from filters.firstSeenFrom (a computed ISO timestamp)
  // so the <select> has a stable value to match against instead of
  // re-deriving "how many weeks ago" from a timestamp that drifts by the
  // millisecond every time weeksAgoIso() is called.
  const [periodWeeks, setPeriodWeeks] = useState<number | null>(null)

  // Debounce free-text search so every keystroke doesn't trigger a request.
  useEffect(() => {
    const timeout = setTimeout(() => {
      setFilters((current) => ({ ...current, searchText: searchDraft || undefined }))
      setPage(1)
    }, 300)
    return () => clearTimeout(timeout)
  }, [searchDraft])

  const { data, isLoading, isError } = useListings({ ...filters, page, pageSize: PAGE_SIZE })
  const { data: sources } = useSources()

  function updateFilter<K extends keyof ListingQueryParams>(key: K, value: ListingQueryParams[K]) {
    setFilters((current) => ({ ...current, [key]: value }))
    setPage(1)
  }

  function resetFilters() {
    setFilters(emptyFilters)
    setSearchDraft('')
    setPeriodWeeks(null)
    setPage(1)
  }

  function updatePeriod(weeks: number | null) {
    setPeriodWeeks(weeks)
    updateFilter('firstSeenFrom', weeks ? weeksAgoIso(weeks) : undefined)
  }

  function togglePropertyType(value: string) {
    const current = filters.propertyTypes ?? []
    const next = current.includes(value) ? current.filter((v) => v !== value) : [...current, value]
    updateFilter('propertyTypes', next.length > 0 ? next : undefined)
  }

  return (
    <section className="listings-page">
      <div className="listings-page-header">
        <h1>Annonces</h1>
        <div className="listings-view-toggle">
          <button
            type="button"
            className={viewMode === 'grid' ? 'active' : ''}
            onClick={() => setViewMode('grid')}
          >
            Grille
          </button>
          <button
            type="button"
            className={viewMode === 'table' ? 'active' : ''}
            onClick={() => setViewMode('table')}
          >
            Tableau
          </button>
        </div>
      </div>

      <div className="listings-filters">
        <input
          type="search"
          placeholder="Rechercher (titre, description, adresse)"
          value={searchDraft}
          onChange={(e) => setSearchDraft(e.target.value)}
          className="listings-filters-search"
        />

        <CommuneMultiSelect
          selectedPostalCodes={filters.postalCodes ?? []}
          onChange={(postalCodes) => updateFilter('postalCodes', postalCodes.length > 0 ? postalCodes : undefined)}
        />

        <div className="listings-filters-property-types">
          {PROPERTY_TYPE_OPTIONS.map((option) => (
            <button
              key={option.value}
              type="button"
              className={filters.propertyTypes?.includes(option.value) ? 'active' : ''}
              onClick={() => togglePropertyType(option.value)}
            >
              {option.label}
            </button>
          ))}
        </div>

        <input
          type="number"
          placeholder="Prix min"
          value={filters.minimumPrice ?? ''}
          onChange={(e) => updateFilter('minimumPrice', e.target.value ? Number(e.target.value) : undefined)}
        />
        <input
          type="number"
          placeholder="Prix max"
          value={filters.maximumPrice ?? ''}
          onChange={(e) => updateFilter('maximumPrice', e.target.value ? Number(e.target.value) : undefined)}
        />
        <input
          type="number"
          placeholder="Logements min"
          value={filters.minimumUnits ?? ''}
          onChange={(e) => updateFilter('minimumUnits', e.target.value ? Number(e.target.value) : undefined)}
        />
        <input
          type="number"
          placeholder="Score min"
          value={filters.minimumScore ?? ''}
          onChange={(e) => updateFilter('minimumScore', e.target.value ? Number(e.target.value) : undefined)}
        />

        <select
          value={filters.riskLevel ?? ''}
          onChange={(e) => updateFilter('riskLevel', e.target.value || undefined)}
        >
          <option value="">Risque (tous)</option>
          <option value="Low">Faible</option>
          <option value="Medium">Modere</option>
          <option value="High">Eleve</option>
        </select>

        <select value={filters.source ?? ''} onChange={(e) => updateFilter('source', e.target.value || undefined)}>
          <option value="">Source (toutes)</option>
          {sources?.map((source) => (
            <option key={source.id} value={source.name}>
              {source.name}
            </option>
          ))}
        </select>

        <select
          value={filters.saleType ?? ''}
          onChange={(e) => updateFilter('saleType', e.target.value || undefined)}
        >
          <option value="">Type de vente (tous)</option>
          <option value="RegularSale">Vente de gre a gre</option>
          <option value="PublicSale">Vente publique</option>
        </select>

        <select
          value={filters.pebRating ?? ''}
          onChange={(e) => updateFilter('pebRating', e.target.value || undefined)}
        >
          <option value="">PEB (tous)</option>
          {['A', 'B', 'C', 'D', 'E', 'F', 'G'].map((rating) => (
            <option key={rating} value={rating}>
              {rating}
            </option>
          ))}
        </select>

        <label className="listings-filters-checkbox">
          <input
            type="checkbox"
            checked={filters.hasGarage ?? false}
            onChange={(e) => updateFilter('hasGarage', e.target.checked || undefined)}
          />
          Garage
        </label>

        <label className="listings-filters-checkbox">
          <input
            type="checkbox"
            checked={filters.isActive ?? false}
            onChange={(e) => updateFilter('isActive', e.target.checked || undefined)}
          />
          Actives uniquement
        </label>

        <select
          value={periodWeeks ?? ''}
          onChange={(e) => updatePeriod(e.target.value ? Number(e.target.value) : null)}
        >
          {PERIOD_OPTIONS.map((option) => (
            <option key={option.label} value={option.weeks ?? ''}>
              {option.label}
            </option>
          ))}
        </select>

        <select
          value={filters.sortBy ?? 'firstSeenAt'}
          onChange={(e) => updateFilter('sortBy', e.target.value)}
        >
          <option value="firstSeenAt">Trier : date de detection</option>
          <option value="price">Trier : prix</option>
          <option value="score">Trier : score</option>
          <option value="livingarea">Trier : surface</option>
        </select>

        <button type="button" className="listings-filters-reset" onClick={resetFilters}>
          Reinitialiser
        </button>
      </div>

      {isLoading && <p className="listings-status">Chargement...</p>}
      {isError && <p className="listings-status">Impossible de charger les annonces.</p>}

      {data && data.items.length === 0 && (
        <p className="listings-status">Aucune annonce ne correspond a ces filtres.</p>
      )}

      {data && data.items.length > 0 && (
        <>
          {viewMode === 'grid' ? (
            <div className="listings-grid">
              {data.items.map((listing) => (
                <PropertyCard key={listing.id} listing={listing} />
              ))}
            </div>
          ) : (
            <ListingsTable listings={data.items} />
          )}

          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </section>
  )
}
