import { QueryClient } from '@tanstack/react-query'

/**
 * Shared TanStack Query client for the whole application.
 * Individual query hooks (listings, sources, search profiles, ...)
 * are added incrementally alongside the features that need them.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
})
