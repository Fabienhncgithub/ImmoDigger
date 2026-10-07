import type { ListingQueryParams, SearchProfile } from '../types'

/**
 * Maps a search profile's criteria onto the listings page's filter shape,
 * so "voir les annonces correspondantes" opens a prefiltered list instead
 * of an empty search. Keep this mapping exhaustive so the count shown on
 * a profile card matches the prefiltered listings page it opens.
 */
export function searchProfileToListingFilters(profile: SearchProfile): ListingQueryParams {
  return {
    postalCodes: profile.postalCodes.length > 0 ? profile.postalCodes : undefined,
    propertyTypes: profile.propertyTypes.length > 0 ? profile.propertyTypes : undefined,
    maximumPrice: profile.maximumPrice ?? undefined,
    minimumUnits: profile.minimumUnitCount ?? undefined,
    minimumScore: profile.minimumOpportunityScore ?? undefined,
    minimumGrossYield: profile.minimumGrossYield ?? undefined,
    minimumLivingArea: profile.minimumLivingArea ?? undefined,
    includePublicSales: profile.includePublicSales ? undefined : false,
    hasGarage: profile.requireGarage ? true : undefined,
    isActive: true,
  }
}
