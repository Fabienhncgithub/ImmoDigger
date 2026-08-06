/** The 19 communes of the Brussels-Capital Region, for the commune/postal-code autocomplete. */
export interface Commune {
  postalCode: string
  name: string
}

export const BRUSSELS_COMMUNES: Commune[] = [
  { postalCode: '1000', name: 'Bruxelles' },
  { postalCode: '1020', name: 'Laeken (Bruxelles)' },
  { postalCode: '1120', name: 'Neder-Over-Heembeek (Bruxelles)' },
  { postalCode: '1130', name: 'Haren (Bruxelles)' },
  { postalCode: '1030', name: 'Schaerbeek' },
  { postalCode: '1040', name: 'Etterbeek' },
  { postalCode: '1050', name: 'Ixelles' },
  { postalCode: '1060', name: 'Saint-Gilles' },
  { postalCode: '1070', name: 'Anderlecht' },
  { postalCode: '1080', name: 'Molenbeek-Saint-Jean' },
  { postalCode: '1081', name: 'Koekelberg' },
  { postalCode: '1082', name: 'Berchem-Sainte-Agathe' },
  { postalCode: '1083', name: 'Ganshoren' },
  { postalCode: '1090', name: 'Jette' },
  { postalCode: '1140', name: 'Evere' },
  { postalCode: '1150', name: 'Woluwe-Saint-Pierre' },
  { postalCode: '1160', name: 'Auderghem' },
  { postalCode: '1170', name: 'Watermael-Boitsfort' },
  { postalCode: '1180', name: 'Uccle' },
  { postalCode: '1190', name: 'Forest' },
  { postalCode: '1200', name: 'Woluwe-Saint-Lambert' },
  { postalCode: '1210', name: 'Saint-Josse-ten-Noode' },
]

export function searchCommunes(query: string): Commune[] {
  const normalized = query.trim().toLowerCase()
  if (!normalized) return []

  return BRUSSELS_COMMUNES.filter(
    (commune) =>
      commune.postalCode.startsWith(normalized) || commune.name.toLowerCase().includes(normalized),
  ).slice(0, 8)
}
