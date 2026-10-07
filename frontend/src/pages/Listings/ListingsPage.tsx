import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useListings } from '../../hooks/useListings'
import { useSources } from '../../hooks/useSources'
import { PropertyCard } from '../../components/PropertyCard/PropertyCard'
import { Pagination } from '../../components/Pagination/Pagination'
import { CommuneMultiSelect } from '../../components/CommuneMultiSelect/CommuneMultiSelect'
import { toQueryString } from '../../api/client'
import { MARKET_AGE_AGING_DAYS, MARKET_AGE_STALE_DAYS, URBANISTIC_STATUS_LABELS } from '../../utils/format'
import type { ListingQueryParams } from '../../types'
import './ListingsPage.css'

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
 * 0 means "today", which is what the dashboard's "Nouvelles aujourd'hui"
 * tile links to.
 */
const PERIOD_OPTIONS = [
  { label: 'Periode (toutes)', weeks: null },
  { label: 'Aujourd’hui', weeks: 0 },
  { label: '1 semaine', weeks: 1 },
  { label: '2 semaines', weeks: 2 },
  { label: '4 semaines', weeks: 4 },
  { label: '3 mois', weeks: 13 },
]

function periodStartIso(weeks: number | null): string | undefined {
  if (weeks === null) return undefined

  const date = new Date()
  if (weeks === 0) {
    // UTC midnight, like the dashboard's own "new today" count.
    date.setUTCHours(0, 0, 0, 0)
  } else {
    date.setDate(date.getDate() - weeks * 7)
  }
  return date.toISOString()
}

function periodFromSearchParams(params: URLSearchParams): number | null {
  const value = params.get('weeks')
  return value === null || value === '' || Number.isNaN(Number(value)) ? null : Number(value)
}

const ARRAY_KEYS = ['cities', 'postalCodes', 'propertyTypes'] as const
const NUMBER_KEYS = [
  'minimumPrice',
  'maximumPrice',
  'minimumUnits',
  'minimumScore',
  'minimumGrossYield',
  'minimumLivingArea',
  'minimumAgeDays',
] as const
const BOOLEAN_KEYS = ['isActive', 'excludeDemo', 'hasGarage', 'includePublicSales'] as const
const STRING_KEYS = ['riskLevel', 'urbanisticStatus', 'source', 'saleType', 'pebRating', 'searchText', 'sortBy'] as const

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
  const [periodWeeks, setPeriodWeeks] = useState<number | null>(() => periodFromSearchParams(searchParams))

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
    const firstSeenFrom = periodStartIso(periodWeeks)
    const query = toQueryString({ ...filters, firstSeenFrom, weeks: periodWeeks ?? undefined, page })
    setSearchParams(query.slice(1), { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters, periodWeeks, page])

  const firstSeenFrom = periodStartIso(periodWeeks)
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

  const activeAdvancedFilterCount = [
    ...(filters.propertyTypes ?? []),
    filters.minimumPrice,
    filters.maximumPrice,
    filters.minimumUnits,
    filters.minimumScore,
    filters.minimumGrossYield,
    filters.minimumLivingArea,
    filters.minimumAgeDays,
    filters.riskLevel,
    filters.urbanisticStatus,
    filters.source,
    filters.saleType,
    filters.pebRating,
    filters.hasGarage ? 'garage' : null,
    filters.isActive ? 'active' : null,
    filters.excludeDemo ? 'without-demo' : null,
    filters.includePublicSales === false ? 'without-public-sales' : null,
    periodWeeks,
    filters.sortBy !== 'firstSeenAt' ? filters.sortBy : null,
  ].filter((value) => value !== undefined && value !== null && value !== '').length

  return (
    <section className="listings-page">
      <div className="listings-page-header">
        <div>
          <h1>Annonces</h1>
          <p>Les informations essentielles d’abord. Ouvrez ensuite la source ou la fiche complète.</p>
        </div>
      </div>

      <div className="listings-filter-panel">
        <div className="listings-quick-filters">
          <label className="listings-filter-field">
            <span>Recherche</span>
            <input
              type="search"
              placeholder="Titre, adresse ou description"
              value={searchDraft}
              onChange={(e) => setSearchDraft(e.target.value)}
            />
          </label>

          <div className="listings-filter-field">
            <span>Localisation</span>
            <CommuneMultiSelect
              selectedPostalCodes={filters.postalCodes ?? []}
              onChange={(postalCodes) => updateFilter('postalCodes', postalCodes.length > 0 ? postalCodes : undefined)}
            />
          </div>
        </div>

        <details className="listings-advanced-filters">
          <summary>
            <span>
              <strong>Filtres avancés</strong>
              <small>Type de bien, budget, caractéristiques et tri</small>
            </span>
            <span className="listings-filter-count">
              {activeAdvancedFilterCount > 0 ? `${activeAdvancedFilterCount} actif${activeAdvancedFilterCount > 1 ? 's' : ''}` : 'Aucun actif'}
            </span>
            <span className="listings-filter-toggle" aria-hidden="true">+</span>
          </summary>

          <div className="listings-advanced-filter-content">
            <fieldset className="listings-filter-group listings-filter-group--wide">
              <legend>Type de bien</legend>
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
            </fieldset>

            <fieldset className="listings-filter-group">
              <legend>Budget et caractéristiques</legend>
              <div className="listings-filter-grid">
                <label>Prix minimum<input type="number" placeholder="0 €" value={filters.minimumPrice ?? ''} onChange={(e) => updateFilter('minimumPrice', e.target.value ? Number(e.target.value) : undefined)} /></label>
                <label>Prix maximum<input type="number" placeholder="Sans limite" value={filters.maximumPrice ?? ''} onChange={(e) => updateFilter('maximumPrice', e.target.value ? Number(e.target.value) : undefined)} /></label>
                <label>Logements minimum<input type="number" placeholder="0" value={filters.minimumUnits ?? ''} onChange={(e) => updateFilter('minimumUnits', e.target.value ? Number(e.target.value) : undefined)} /></label>
                <label>Surface minimum<input type="number" min="0" placeholder="0 m²" value={filters.minimumLivingArea ?? ''} onChange={(e) => updateFilter('minimumLivingArea', e.target.value ? Number(e.target.value) : undefined)} /></label>
                <label>Indice minimum<input type="number" placeholder="0 / 100" value={filters.minimumScore ?? ''} onChange={(e) => updateFilter('minimumScore', e.target.value ? Number(e.target.value) : undefined)} /></label>
                <label>Rendement minimum<input type="number" min="0" step="0.1" placeholder="0 %" value={filters.minimumGrossYield ?? ''} onChange={(e) => updateFilter('minimumGrossYield', e.target.value ? Number(e.target.value) : undefined)} /></label>
              </div>
            </fieldset>

            <fieldset className="listings-filter-group">
              <legend>Source et analyse</legend>
              <div className="listings-filter-grid">
                <label>Vigilance<select value={filters.riskLevel ?? ''} onChange={(e) => updateFilter('riskLevel', e.target.value || undefined)}><option value="">Toutes</option><option value="Low">Aucune alerte détectée</option><option value="Medium">À vérifier</option><option value="High">Alerte majeure</option></select></label>
                <label>Urbanisme<select value={filters.urbanisticStatus ?? ''} onChange={(e) => updateFilter('urbanisticStatus', e.target.value || undefined)}><option value="">Tous</option>{Object.entries(URBANISTIC_STATUS_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
                <label>Source<select value={filters.source ?? ''} onChange={(e) => updateFilter('source', e.target.value || undefined)}><option value="">Toutes</option>{sources?.map((source) => <option key={source.id} value={source.name}>{source.name}</option>)}</select></label>
                <label>Type de vente<select value={filters.saleType ?? ''} onChange={(e) => updateFilter('saleType', e.target.value || undefined)}><option value="">Tous</option><option value="RegularSale">Vente de gré à gré</option><option value="PublicSale">Vente publique</option></select></label>
                <label>PEB<select value={filters.pebRating ?? ''} onChange={(e) => updateFilter('pebRating', e.target.value || undefined)}><option value="">Tous</option>{['A', 'B', 'C', 'D', 'E', 'F', 'G'].map((rating) => <option key={rating} value={rating}>{rating}</option>)}</select></label>
              </div>
            </fieldset>

            <fieldset className="listings-filter-group listings-filter-group--wide">
              <legend>Affichage</legend>
              <div className="listings-filter-options">
                <label><input type="checkbox" checked={filters.hasGarage ?? false} onChange={(e) => updateFilter('hasGarage', e.target.checked || undefined)} /> Garage</label>
                <label><input type="checkbox" checked={filters.isActive ?? false} onChange={(e) => updateFilter('isActive', e.target.checked || undefined)} /> Actives uniquement</label>
                <label><input type="checkbox" checked={filters.excludeDemo ?? false} onChange={(e) => updateFilter('excludeDemo', e.target.checked || undefined)} /> Masquer les démos</label>
                <label><input type="checkbox" checked={filters.includePublicSales !== false} onChange={(e) => updateFilter('includePublicSales', e.target.checked ? undefined : false)} /> Inclure les ventes publiques</label>
              </div>
            </fieldset>

            <div className="listings-filter-footer">
              <label>Période<select value={periodWeeks ?? ''} onChange={(e) => updatePeriod(e.target.value === '' ? null : Number(e.target.value))}>{PERIOD_OPTIONS.map((option) => <option key={option.label} value={option.weeks ?? ''}>{option.label}</option>)}</select></label>
              <label>En vente depuis<select value={filters.minimumAgeDays ?? ''} onChange={(e) => updateFilter('minimumAgeDays', e.target.value ? Number(e.target.value) : undefined)}><option value="">Peu importe</option><option value="30">Plus d’1 mois</option><option value={MARKET_AGE_AGING_DAYS}>Plus de 2 mois</option><option value={MARKET_AGE_STALE_DAYS}>Plus de 4 mois</option></select></label>
              <label>Trier par<select value={filters.sortBy ?? 'firstSeenAt'} onChange={(e) => updateFilter('sortBy', e.target.value)}><option value="firstSeenAt">Date de détection</option><option value="price">Prix</option><option value="score">Indice</option><option value="livingarea">Surface</option></select></label>
              <button type="button" className="listings-filters-reset" onClick={resetFilters}>Réinitialiser les filtres</button>
            </div>
          </div>
        </details>
      </div>

      <div className="listings-results-heading">
        <strong>{data ? `${data.totalCount} annonce${data.totalCount > 1 ? 's' : ''}` : 'Annonces'}</strong>
        <span>{activeAdvancedFilterCount > 0 || searchDraft || (filters.postalCodes?.length ?? 0) > 0 ? 'Résultats filtrés' : 'Toutes les sources actives'}</span>
      </div>

      {isLoading && <p className="listings-status">Chargement...</p>}
      {isError && <p className="listings-status">Impossible de charger les annonces.</p>}

      {data && data.items.length === 0 && (
        <p className="listings-status">Aucune annonce ne correspond a ces filtres.</p>
      )}

      {data && data.items.length > 0 && (
        <>
          <div className="listings-grid">
            {data.items.map((listing) => (
              <PropertyCard key={listing.id} listing={listing} />
            ))}
          </div>

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
