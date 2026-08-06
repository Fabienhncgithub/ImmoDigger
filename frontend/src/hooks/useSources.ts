import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { collectionApi, sourcesApi } from '../api/sources'
import type { UpdateSourceRequest } from '../types'

const sourcesQueryKey = ['sources'] as const
const collectionStatusQueryKey = ['collection', 'status'] as const

export function useSources() {
  return useQuery({ queryKey: sourcesQueryKey, queryFn: sourcesApi.list })
}

export function useUpdateSource() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateSourceRequest }) => sourcesApi.update(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: sourcesQueryKey }),
  })
}

export function useCollectionStatus() {
  return useQuery({ queryKey: collectionStatusQueryKey, queryFn: collectionApi.status })
}

export function useRunCollection() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: collectionApi.run,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sourcesQueryKey })
      queryClient.invalidateQueries({ queryKey: collectionStatusQueryKey })
    },
  })
}
