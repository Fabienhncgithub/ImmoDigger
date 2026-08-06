import { apiClient } from './client'
import type { SearchProfile, SearchProfileRequest } from '../types'

export const searchProfilesApi = {
  list: () => apiClient.get<SearchProfile[]>('/search-profiles'),
  create: (request: SearchProfileRequest) => apiClient.post<SearchProfile>('/search-profiles', request),
  update: (id: string, request: SearchProfileRequest) =>
    apiClient.put<SearchProfile>(`/search-profiles/${id}`, request),
  remove: (id: string) => apiClient.delete<void>(`/search-profiles/${id}`),
}
