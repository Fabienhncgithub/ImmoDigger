import type { UrbanisticStatus } from '../types'

const currencyFormatter = new Intl.NumberFormat('fr-BE', {
  style: 'currency',
  currency: 'EUR',
  maximumFractionDigits: 0,
})

const dateFormatter = new Intl.DateTimeFormat('fr-BE', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

const dateTimeFormatter = new Intl.DateTimeFormat('fr-BE', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

export function formatPrice(value: number | null): string {
  return value === null ? '—' : currencyFormatter.format(value)
}

export function formatArea(value: number | null): string {
  return value === null ? '—' : `${value.toLocaleString('fr-BE')} m²`
}

export function formatPercent(value: number | null, fractionDigits = 1): string {
  return value === null ? '—' : `${value.toFixed(fractionDigits)} %`
}

/** The ImmoDigger comparison index is out of 100 points, not a market-value percentage. */
export function formatScore(value: number | null): string {
  return value === null ? '—' : `${Math.round(value)}/100`
}

export function formatDate(value: string | null): string {
  return value ? dateFormatter.format(new Date(value)) : '—'
}

export function formatDateTime(value: string | null): string {
  return value ? dateTimeFormatter.format(new Date(value)) : '—'
}

const SALE_TYPE_LABELS: Record<string, string> = {
  RegularSale: 'Vente de gré à gré',
  PublicSale: 'Vente publique',
}

export function formatSaleType(saleType: string): string {
  return SALE_TYPE_LABELS[saleType] ?? saleType
}

export const URBANISTIC_STATUS_LABELS: Record<UrbanisticStatus, string> = {
  Infraction: 'Infraction ou à régulariser',
  Compliant: 'Sans infraction (selon l’annonce)',
  Unknown: 'À voir (non précisé)',
}

export function formatUrbanisticStatus(status: UrbanisticStatus): string {
  return URBANISTIC_STATUS_LABELS[status] ?? URBANISTIC_STATUS_LABELS.Unknown
}

// How long a listing has been on the market, from the earliest date we know.
// Past two months it is worth noticing; past four it has clearly not found
// a buyer at that price, which is a negotiation signal.
export const MARKET_AGE_AGING_DAYS = 60
export const MARKET_AGE_STALE_DAYS = 120

export interface MarketAge {
  days: number
  /** e.g. "12 jours", "3 mois". */
  duration: string
  tone: 'recent' | 'aging' | 'stale'
}

export function getMarketAge(listedSince: string, now: Date = new Date()): MarketAge {
  const days = Math.max(0, Math.floor((now.getTime() - new Date(listedSince).getTime()) / 86_400_000))
  const duration = days < MARKET_AGE_AGING_DAYS
    ? `${days} jour${days > 1 ? 's' : ''}`
    : `${Math.floor(days / 30)} mois`
  const tone = days >= MARKET_AGE_STALE_DAYS ? 'stale' : days >= MARKET_AGE_AGING_DAYS ? 'aging' : 'recent'

  return { days, duration, tone }
}

const PROPERTY_TYPE_LABELS: Record<string, string> = {
  IncomeBuilding: 'Immeuble de rapport',
  House: 'Maison',
  Apartment: 'Appartement',
  Warehouse: 'Entrepôt',
  Office: 'Bureau',
  Land: 'Terrain',
  Garage: 'Garage',
  Other: 'Autre',
}

export function formatPropertyType(propertyType: string): string {
  return PROPERTY_TYPE_LABELS[propertyType] ?? propertyType
}

export interface DisplayPrice {
  label: string
  value: string
  isAuction: boolean
}

/**
 * A public-sale (vente publique/enchere) listing's "price" is a starting
 * bid or a live current bid, not a fixed asking price - showing it under a
 * flat "Prix" label is misleading. This picks the right label and value:
 * the current bid when one has been recorded, otherwise the starting price
 * ("mise a prix"), otherwise the regular asking price.
 */
export function getDisplayPrice(saleType: string, askingPrice: number | null, currentBid: number | null): DisplayPrice {
  const isAuction = saleType === 'PublicSale'

  if (!isAuction) {
    return { label: 'Prix demande', value: formatPrice(askingPrice), isAuction: false }
  }

  if (currentBid !== null) {
    return { label: 'Enchere actuelle', value: formatPrice(currentBid), isAuction: true }
  }

  return { label: 'Mise a prix', value: formatPrice(askingPrice), isAuction: true }
}
