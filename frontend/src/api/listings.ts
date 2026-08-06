import { apiClient, toQueryString } from './client'
import type {
  AnalyzeListingResponse,
  ListingDetail,
  ListingQueryParams,
  ListingSummary,
  PagedResult,
  PriceHistoryEntry,
  UpdateListingRequest,
} from '../types'

export const listingsApi = {
  list: (params: ListingQueryParams) =>
    apiClient.get<PagedResult<ListingSummary>>(`/listings${toQueryString(params)}`),

  getById: (id: string) => apiClient.get<ListingDetail>(`/listings/${id}`),

  analyze: (id: string) => apiClient.post<AnalyzeListingResponse>(`/listings/${id}/analyze`),

  update: (id: string, request: UpdateListingRequest) =>
    apiClient.patch<ListingDetail>(`/listings/${id}`, request),

  markReviewed: (id: string) => apiClient.post<ListingDetail>(`/listings/${id}/mark-reviewed`),

  getPriceHistory: (id: string) => apiClient.get<PriceHistoryEntry[]>(`/listings/${id}/price-history`),
}
