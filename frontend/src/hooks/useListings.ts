import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { listingsApi } from '../api/listings'
import type { ListingQueryParams, UpdateListingRequest } from '../types'

export const listingsQueryKeys = {
  all: ['listings'] as const,
  list: (params: ListingQueryParams) => ['listings', 'list', params] as const,
  detail: (id: string) => ['listings', 'detail', id] as const,
  priceHistory: (id: string) => ['listings', 'price-history', id] as const,
}

export function useListings(params: ListingQueryParams) {
  return useQuery({
    queryKey: listingsQueryKeys.list(params),
    queryFn: () => listingsApi.list(params),
  })
}

export function useListing(id: string | undefined) {
  return useQuery({
    queryKey: listingsQueryKeys.detail(id ?? ''),
    queryFn: () => listingsApi.getById(id as string),
    enabled: Boolean(id),
  })
}

export function usePriceHistory(id: string | undefined) {
  return useQuery({
    queryKey: listingsQueryKeys.priceHistory(id ?? ''),
    queryFn: () => listingsApi.getPriceHistory(id as string),
    enabled: Boolean(id),
  })
}

function useInvalidateListing(id: string) {
  const queryClient = useQueryClient()
  return () => {
    queryClient.invalidateQueries({ queryKey: listingsQueryKeys.detail(id) })
    queryClient.invalidateQueries({ queryKey: listingsQueryKeys.all })
  }
}

export function useAnalyzeListing(id: string) {
  const invalidate = useInvalidateListing(id)
  return useMutation({
    mutationFn: () => listingsApi.analyze(id),
    onSuccess: invalidate,
  })
}

export function useUpdateListing(id: string) {
  const invalidate = useInvalidateListing(id)
  return useMutation({
    mutationFn: (request: UpdateListingRequest) => listingsApi.update(id, request),
    onSuccess: invalidate,
  })
}

export function useMarkReviewed(id: string) {
  const invalidate = useInvalidateListing(id)
  return useMutation({
    mutationFn: () => listingsApi.markReviewed(id),
    onSuccess: invalidate,
  })
}

/**
 * Permanently deletes a listing the user isn't interested in. Note: if
 * it's still live on its source next time that source is collected, it
 * will come back as a "new" listing - deduplication has no record that it
 * was deliberately removed.
 */
export function useDeleteListing() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => listingsApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: listingsQueryKeys.all })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

/** The index's criteria and scales never change at runtime, so they are fetched once. */
export function useIndexMethodology() {
  return useQuery({
    queryKey: ['index-methodology'],
    queryFn: listingsApi.getIndexMethodology,
    staleTime: Infinity,
  })
}
