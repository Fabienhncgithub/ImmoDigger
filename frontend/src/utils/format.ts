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

/** The opportunity score is out of 100 points, not a percentage - matches ScoreBadge's "N/100" format. */
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
  RegularSale: 'Vente de gre a gre',
  PublicSale: 'Vente publique',
}

export function formatSaleType(saleType: string): string {
  return SALE_TYPE_LABELS[saleType] ?? saleType
}

const PROPERTY_TYPE_LABELS: Record<string, string> = {
  IncomeBuilding: 'Immeuble de rapport',
  House: 'Maison',
  Apartment: 'Appartement',
  Warehouse: 'Entrepot',
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
