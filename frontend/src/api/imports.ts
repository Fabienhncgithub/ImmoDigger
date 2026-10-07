import { apiClient } from './client'
import type { ImportUrlRequest, ListingDetail } from '../types'

export const importsApi = {
  importUrl: (request: ImportUrlRequest) =>
    apiClient.post<ListingDetail>('/import/url', request),
}
