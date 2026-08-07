using ImmoDigger.Application.DTOs;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Mapping;

public static class SearchProfileMappingExtensions
{
    public static SearchProfileDto ToDto(this SearchProfile profile, int matchingListingsCount) => new(
        profile.Id,
        profile.Name,
        profile.MaximumPrice,
        profile.MinimumGrossYield,
        profile.MinimumUnitCount,
        profile.MinimumLivingArea,
        profile.RequireGarage,
        profile.IncludePublicSales,
        profile.PostalCodes,
        profile.PropertyTypes,
        profile.MinimumOpportunityScore,
        profile.IsEnabled,
        matchingListingsCount);

    public static void ApplyRequest(this SearchProfile profile, SearchProfileRequest request)
    {
        profile.Name = request.Name;
        profile.MaximumPrice = request.MaximumPrice;
        profile.MinimumGrossYield = request.MinimumGrossYield;
        profile.MinimumUnitCount = request.MinimumUnitCount;
        profile.MinimumLivingArea = request.MinimumLivingArea;
        profile.RequireGarage = request.RequireGarage;
        profile.IncludePublicSales = request.IncludePublicSales;
        profile.PostalCodes = request.PostalCodes;
        profile.PropertyTypes = request.PropertyTypes;
        profile.MinimumOpportunityScore = request.MinimumOpportunityScore;
        profile.IsEnabled = request.IsEnabled;
    }
}
