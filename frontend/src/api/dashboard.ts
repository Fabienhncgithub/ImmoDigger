import { apiClient } from './client'
import type { DashboardSummary } from '../types'

export const dashboardApi = {
  getSummary: () => apiClient.get<DashboardSummary>('/dashboard/summary'),
}
