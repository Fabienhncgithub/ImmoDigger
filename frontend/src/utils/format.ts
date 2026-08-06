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
  ApartmentBuilding: 'Immeuble a appartements',
  House: 'Maison',
}

export function formatPropertyType(propertyType: string): string {
  return PROPERTY_TYPE_LABELS[propertyType] ?? propertyType
}
