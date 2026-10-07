import { useMutation, useQueryClient } from '@tanstack/react-query'
import { importsApi } from '../api/imports'
import type { ImportUrlRequest } from '../types'

export function useImportListing() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: ImportUrlRequest) => importsApi.importUrl(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['listings'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      queryClient.invalidateQueries({ queryKey: ['sources'] })
    },
  })
}
