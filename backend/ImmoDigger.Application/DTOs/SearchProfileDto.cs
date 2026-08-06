namespace ImmoDigger.Application.DTOs;

public sealed record SearchProfileDto(
    Guid Id,
    string Name,
    decimal? MaximumPrice,
    decimal? MinimumGrossYield,
    int? MinimumUnitCount,
    decimal? MinimumLivingArea,
    bool RequireGarage,
    bool IncludePublicSales,
    string[] PostalCodes,
    string[] PropertyTypes,
    decimal? MinimumOpportunityScore,
    bool IsEnabled);

/// <summary>Body for both <c>POST /api/search-profiles</c> and <c>PUT /api/search-profiles/{id}</c> (full replace).</summary>
public sealed record SearchProfileRequest(
    string Name,
    decimal? MaximumPrice,
    decimal? MinimumGrossYield,
    int? MinimumUnitCount,
    decimal? MinimumLivingArea,
    bool RequireGarage,
    bool IncludePublicSales,
    string[] PostalCodes,
    string[] PropertyTypes,
    decimal? MinimumOpportunityScore,
    bool IsEnabled);
