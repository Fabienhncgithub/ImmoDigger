namespace ImmoDigger.Application.DTOs;

/// <summary>Shape used by the listings list/grid view (<c>GET /api/listings</c>).</summary>
public sealed record ListingSummaryDto(
    Guid Id,
    string Source,
    string Title,
    string City,
    string PostalCode,
    decimal? AskingPrice,
    int? UnitCount,
    decimal? LivingArea,
    string? PebRating,
    decimal? EstimatedGrossYield,
    decimal? OpportunityScore,
    string? RiskLevel,
    string SaleType,
    string PropertyType,
    bool IsActive,
    bool IsReviewed,
    DateTime FirstSeenAt);
