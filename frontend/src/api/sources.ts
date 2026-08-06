import { apiClient } from './client'
import type { CollectionStatus, Source, UpdateSourceRequest } from '../types'

export const sourcesApi = {
  list: () => apiClient.get<Source[]>('/sources'),
  update: (id: string, request: UpdateSourceRequest) => apiClient.patch<Source>(`/sources/${id}`, request),
}

export const collectionApi = {
  run: () => apiClient.post<void>('/collection/run'),
  status: () => apiClient.get<CollectionStatus>('/collection/status'),
}
