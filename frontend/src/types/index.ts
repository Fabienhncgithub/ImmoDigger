// Mirrors the backend DTOs (ImmoDigger.Application/DTOs). Field names match
// System.Text.Json's default camelCase serialization.

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

/** What the listing's own text says about its planning situation (see the backend's UrbanisticStatusDetector). */
export type UrbanisticStatus = 'Infraction' | 'Compliant' | 'Unknown'

export interface ListingSummary {
  id: string
  source: string
  title: string
  url: string
  imageUrl: string | null
  description: string
  address: string
  city: string
  postalCode: string
  askingPrice: number | null
  currentBid: number | null
  unitCount: number | null
  bedroomCount: number | null
  bathroomCount: number | null
  officialDocumentCount: number
  livingArea: number | null
  landArea: number | null
  pebRating: string | null
  estimatedGrossYield: number | null
  opportunityScore: number | null
  riskLevel: string | null
  riskSummary: string | null
  saleType: string
  propertyType: string
  isActive: boolean
  isDemo: boolean
  isReviewed: boolean
  firstSeenAt: string
  urbanisticStatus: UrbanisticStatus
  /** Earliest date the listing is known to have been on the market. */
  listedSince: string
  /** How many of the index's six criteria could be evaluated; null when there is no index. */
  indexCriteriaCount: number | null
}

export interface ListingDetail {
  id: string
  source: string
  externalId: string
  url: string
  title: string
  imageUrl: string | null
  description: string
  address: string
  postalCode: string
  city: string
  askingPrice: number | null
  currentBid: number | null
  estimatedFinalPrice: number | null
  saleType: string
  propertyType: string
  bedroomCount: number | null
  bathroomCount: number | null
  officialUnitCount: number | null
  officialUnitCountSourceName: string | null
  officialUnitCountSourceUrl: string | null
  observedUnitCount: number | null
  officialDocuments: ListingDocument[]
  livingArea: number | null
  landArea: number | null
  pebRating: string | null
  pebConsumption: number | null
  electricalInstallationCompliant: boolean | null
  isOccupied: boolean | null
  hasGarage: boolean | null
  hasTerrace: boolean | null
  hasGarden: boolean | null
  cadastralIncome: number | null
  auctionStartDate: string | null
  auctionEndDate: string | null
  firstSeenAt: string
  lastSeenAt: string
  publishedAt: string | null
  isActive: boolean
  isDemo: boolean
  opportunityScore: number | null
  estimatedGrossYield: number | null
  estimatedRenovationCost: number | null
  riskLevel: string | null
  riskSummary: string | null
  estimatedMonthlyRentPerUnit: number | null
  estimatedAcquisitionCosts: number | null
  estimatedRenovationBudget: number | null
  personalNotes: string | null
  isReviewed: boolean
  reviewedAt: string | null
  urbanisticStatus: UrbanisticStatus
  listedSince: string
}

export interface ListingDocument {
  type: string
  name: string
  url: string
}

export interface PriceHistoryEntry {
  id: string
  price: number
  recordedAt: string
}

export interface OpportunityScoreBreakdown {
  totalScore: number | null
  rawScore: number
  availablePoints: number
  dataCompletenessPercentage: number
  evaluatedCriteriaCount: number
  totalCriteriaCount: number
  pricePerSquareMeterScore: number
  pricePerSquareMeterAvailable: boolean
  grossYieldScore: number
  grossYieldAvailable: boolean
  unitCountScore: number
  unitCountAvailable: boolean
  locationScore: number
  locationAvailable: boolean
  energyScore: number
  energyAvailable: boolean
  riskScore: number
  riskAvailable: boolean
  missingData: string[]
  positiveSignals: string[]
  riskSignals: string[]
}

export interface IndexMethodology {
  totalPoints: number
  minimumAvailablePoints: number
  strongOpportunityThreshold: number
  criteria: Array<{
    key: string
    label: string
    maxPoints: number
    basis: string
    countedWhen: string
    steps: Array<{ condition: string; points: number }>
  }>
}

export interface RiskAssessment {
  riskLevel: string
  riskSummary: string
  signals: string[]
}

export interface AnalyzeListingResponse {
  listing: ListingDetail
  scoreBreakdown: OpportunityScoreBreakdown
  riskAssessment: RiskAssessment
}

export interface UpdateListingRequest {
  personalNotes: string | null
  estimatedMonthlyRentPerUnit: number | null
  estimatedAcquisitionCosts: number | null
  estimatedRenovationBudget: number | null
}

export interface ListingQueryParams {
  cities?: string[]
  postalCodes?: string[]
  propertyTypes?: string[]
  minimumPrice?: number
  maximumPrice?: number
  minimumUnits?: number
  minimumScore?: number
  minimumGrossYield?: number
  minimumLivingArea?: number
  includePublicSales?: boolean
  riskLevel?: string
  urbanisticStatus?: string
  source?: string
  saleType?: string
  isActive?: boolean
  excludeDemo?: boolean
  hasGarage?: boolean
  pebRating?: string
  firstSeenFrom?: string
  minimumAgeDays?: number
  searchText?: string
  sortBy?: string
  sortDescending?: boolean
  page?: number
  pageSize?: number
}

export interface ManualListingFields {
  title: string
  description?: string | null
  address?: string | null
  postalCode?: string | null
  city?: string | null
  price?: number | null
  propertyType?: string | null
  imageUrl?: string | null
}

export interface ImportUrlRequest {
  url: string
  manualFallback?: ManualListingFields
}

export interface SearchProfile {
  id: string
  name: string
  maximumPrice: number | null
  minimumGrossYield: number | null
  minimumUnitCount: number | null
  minimumLivingArea: number | null
  requireGarage: boolean
  includePublicSales: boolean
  postalCodes: string[]
  propertyTypes: string[]
  minimumOpportunityScore: number | null
  isEnabled: boolean
  /** How many active listings satisfy this profile's criteria right now (computed server-side). */
  matchingListingsCount: number
}

export type SearchProfileRequest = Omit<SearchProfile, 'id' | 'matchingListingsCount'>

export interface Source {
  id: string
  name: string
  baseUrl: string
  isEnabled: boolean
  /** The hard compliance gate, independent of isEnabled - see ListingSource.Allowed on the backend. */
  allowed: boolean
  collectionMethod: 'Api' | 'Rss' | 'PublicFeed' | 'Html' | 'Email' | 'Manual' | 'Disabled'
  notes: string | null
  pollingIntervalMinutes: number
  lastSuccessfulRunAt: string | null
  lastFailedRunAt: string | null
  lastError: string | null
  listingCount: number
}

export interface UpdateSourceRequest {
  isEnabled: boolean
  pollingIntervalMinutes: number | null
}

export interface CollectionStatus {
  sources: Source[]
  lastRunAt: string | null
}

export interface EmailImportStatus {
  isConfigured: boolean
  autoEnable: boolean
  folder: string
  lookbackDays: number
  supportedSources: string[]
  configurationIssues: string[]
}

export interface EmailImportConnectionTest {
  success: boolean
  messagesFound: number
  message: string
}

export interface DashboardSummary {
  newListingsToday: number
  activeListingsCount: number
  realActiveListingsCount: number
  demoActiveListingsCount: number
  pricedActiveListingsCount: number
  scoredActiveListingsCount: number
  averagePrice: number | null
  averageScore: number | null
  strongOpportunitiesCount: number
  highRiskCount: number
  recentListings: ListingSummary[]
}
