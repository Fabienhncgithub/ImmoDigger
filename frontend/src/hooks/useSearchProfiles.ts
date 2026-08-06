import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { searchProfilesApi } from '../api/searchProfiles'
import type { SearchProfileRequest } from '../types'

const queryKey = ['search-profiles'] as const

export function useSearchProfiles() {
  return useQuery({ queryKey, queryFn: searchProfilesApi.list })
}

export function useCreateSearchProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: SearchProfileRequest) => searchProfilesApi.create(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })
}

export function useUpdateSearchProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: SearchProfileRequest }) =>
      searchProfilesApi.update(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })
}

export function useDeleteSearchProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => searchProfilesApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  })
}
