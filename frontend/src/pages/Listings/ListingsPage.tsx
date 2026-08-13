import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useListings } from '../../hooks/useListings'
import { useSources } from '../../hooks/useSources'
import { PropertyCard } from '../../components/PropertyCard/PropertyCard'
import { ListingsTable } from '../../components/ListingsTable/ListingsTable'
import { Pagination } from '../../components/Pagination/Pagination'
import { CommuneMultiSelect } from '../../components/CommuneMultiSelect/CommuneMultiSelect'
import { toQueryString } from '../../api/client'
import type { ListingQueryParams } from '../../types'
import './ListingsPage.css'

type ViewMode = 'grid' | 'table'

const PAGE_SIZE = 12

const emptyFilters: ListingQueryParams = {
  sortBy: 'firstSeenAt',
  sortDescending: true,
}

// Matches the real vocabulary the collectors actually produce (see
// PropertyListing.PropertyType's doc comment) - "ApartmentBuilding" used
// to be offered here but no real collector has ever emitted it (only the
// fictional demo data did, since fixed to "IncomeBuilding"), so picking
// it silently filtered to nothing.
const PROPERTY_TYPE_OPTIONS = [
  { value: 'IncomeBuilding', label: 'Immeuble de rapport' },
  { value: 'House', label: 'Maison' },
  { value: 'Apartment', label: 'Appartement' },
  { value: 'Warehouse', label: 'Entrepot' },
  { value: 'Office', label: 'Bureau' },
  { value: 'Land', label: 'Terrain' },
  { value: 'Garage', label: 'Garage' },
]

/**
 * "depuis N semaines" options. The picked week count (not the resulting
 * date) is what's persisted in the URL, as its own "weeks" param - the
 * actual cutoff is recomputed relative to "now" every time the page loads,
 * same as a real search would: "last 2 weeks" means 2 weeks before whenever
 * you're looking, not frozen to the exact moment the filter was first set.
 */
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

const ARRAY_KEYS = ['cities', 'postalCodes', 'propertyTypes'] as const
const NUMBER_KEYS = ['minimumPrice', 'maximumPrice', 'minimumUnits', 'minimumScore'] as const
const BOOLEAN_KEYS = ['isActive', 'hasGarage'] as const
const STRING_KEYS = ['riskLevel', 'source', 'saleType', 'pebRating', 'searchText', 'sortBy'] as const

/** Reconstructs filters (everything except the period, tracked separately - see PERIOD_OPTIONS) from the URL's query string. */
function filtersFromSearchParams(params: URLSearchParams): ListingQueryParams {
  const result: ListingQueryParams = {}

  for (const key of ARRAY_KEYS) {
    const values = params.getAll(key)
    if (values.length > 0) result[key] = values
  }
  for (const key of NUMBER_KEYS) {
    const value = params.get(key)
    if (value) result[key] = Number(value)
  }
  for (const key of BOOLEAN_KEYS) {
    const value = params.get(key)
    if (value) result[key] = value === 'true'
  }
  for (const key of STRING_KEYS) {
    const value = params.get(key)
    if (value) result[key] = value
  }

  result.sortDescending = params.get('sortDescending') !== 'false'

  return result
}

export function ListingsPage() {
  // A search profile's "voir les annonces correspondantes" link navigates
  // here with its filters already encoded in the URL (see
  // searchProfileToListingFilters + toQueryString in SearchProfilesPage) -
  // deliberately not via router navigation state, which the URL-sync
  // effect below would otherwise wipe out before it could ever be read.
  const [searchParams, setSearchParams] = useSearchParams()

  const [filters, setFilters] = useState<ListingQueryParams>(() => {
    const fromUrl = filtersFromSearchParams(searchParams)
    return Object.keys(fromUrl).length > 0 ? { sortBy: 'firstSeenAt', sortDescending: true, ...fromUrl } : emptyFilters
  })
  const [searchDraft, setSearchDraft] = useState(filters.searchText ?? '')
  const [page, setPage] = useState(() => Number(searchParams.get('page')) || 1)
  const [viewMode, setViewMode] = useState<ViewMode>('grid')
  const [periodWeeks, setPeriodWeeks] = useState<number | null>(() => Number(searchParams.get('weeks')) || null)

  // Debounce free-text search so every keystroke doesn't trigger a request.
  useEffect(() => {
    const timeout = setTimeout(() => {
      setFilters((current) => ({ ...current, searchText: searchDraft || undefined }))
      setPage(1)
    }, 300)
    return () => clearTimeout(timeout)
  }, [searchDraft])

  // Keeps the URL in sync so filters (including the period) survive a
  // refresh, a bookmark, or sharing the link - not just kept in memory.
  useEffect(() => {
    const firstSeenFrom = periodWeeks ? weeksAgoIso(periodWeeks) : undefined
    const query = toQueryString({ ...filters, firstSeenFrom, weeks: periodWeeks ?? undefined, page })
    setSearchParams(query.slice(1), { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters, periodWeeks, page])

  const firstSeenFrom = periodWeeks ? weeksAgoIso(periodWeeks) : undefined
  const { data, isLoading, isError } = useListings({ ...filters, firstSeenFrom, page, pageSize: PAGE_SIZE })
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
    setPage(1)
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
