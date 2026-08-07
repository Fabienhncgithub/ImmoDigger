import type { ListingQueryParams, SearchProfile } from '../types'

/**
 * Maps a search profile's criteria onto the listings page's filter shape,
 * so "voir les annonces correspondantes" opens a prefiltered list instead
 * of an empty search. Only covers the criteria the listings filter bar
 * itself supports (postal codes, property types, max price, min units,
 * min score, garage) - minimum yield/living area and the public-sale
 * toggle aren't filterable there yet, so the server-side matching count
 * on the profile card can be slightly stricter than what this link shows.
 */
export function searchProfileToListingFilters(profile: SearchProfile): ListingQueryParams {
  return {
    postalCodes: profile.postalCodes.length > 0 ? profile.postalCodes : undefined,
    propertyTypes: profile.propertyTypes.length > 0 ? profile.propertyTypes : undefined,
    maximumPrice: profile.maximumPrice ?? undefined,
    minimumUnits: profile.minimumUnitCount ?? undefined,
    minimumScore: profile.minimumOpportunityScore ?? undefined,
    hasGarage: profile.requireGarage ? true : undefined,
    isActive: true,
  }
}
