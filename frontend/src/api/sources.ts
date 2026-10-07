import { apiClient } from './client'
import type {
  CollectionStatus,
  EmailImportConnectionTest,
  EmailImportStatus,
  Source,
  UpdateSourceRequest,
} from '../types'

export const sourcesApi = {
  list: () => apiClient.get<Source[]>('/sources'),
  emailImportStatus: () => apiClient.get<EmailImportStatus>('/sources/email-import/status'),
  testEmailImport: () => apiClient.post<EmailImportConnectionTest>('/sources/email-import/test'),
  update: (id: string, request: UpdateSourceRequest) => apiClient.patch<Source>(`/sources/${id}`, request),
}

export const collectionApi = {
  run: () => apiClient.post<void>('/collection/run'),
  status: () => apiClient.get<CollectionStatus>('/collection/status'),
}
