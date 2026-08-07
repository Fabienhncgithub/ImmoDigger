// Mirrors the backend DTOs (ImmoDigger.Application/DTOs). Field names match
// System.Text.Json's default camelCase serialization.

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ListingSummary {
  id: string
  source: string
  title: string
  imageUrl: string | null
  city: string
  postalCode: string
  askingPrice: number | null
  currentBid: number | null
  unitCount: number | null
  livingArea: number | null
  pebRating: string | null
  estimatedGrossYield: number | null
  opportunityScore: number | null
  riskLevel: string | null
  saleType: string
  propertyType: string
  isActive: boolean
  isReviewed: boolean
  firstSeenAt: string
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
  observedUnitCount: number | null
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
}

export interface PriceHistoryEntry {
  id: string
  price: number
  recordedAt: string
}

export interface OpportunityScoreBreakdown {
  totalScore: number
  pricePerSquareMeterScore: number
  grossYieldScore: number
  unitCountScore: number
  locationScore: number
  energyScore: number
  riskScore: number
  positiveSignals: string[]
  riskSignals: string[]
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
  riskLevel?: string
  source?: string
  saleType?: string
  isActive?: boolean
  hasGarage?: boolean
  pebRating?: string
  firstSeenFrom?: string
  searchText?: string
  sortBy?: string
  sortDescending?: boolean
  page?: number
  pageSize?: number
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

export interface DashboardSummary {
  newListingsToday: number
  activeListingsCount: number
  averagePrice: number | null
  averageScore: number | null
  strongOpportunitiesCount: number
  highRiskCount: number
  recentListings: ListingSummary[]
}
