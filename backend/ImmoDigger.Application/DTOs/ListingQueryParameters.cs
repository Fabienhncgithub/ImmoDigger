namespace ImmoDigger.Application.DTOs;

/// <summary>Filters, sort and pagination for <c>GET /api/listings</c>.</summary>
public sealed record ListingQueryParameters
{
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public decimal? MinimumPrice { get; init; }
    public decimal? MaximumPrice { get; init; }
    public int? MinimumUnits { get; init; }
    public decimal? MinimumScore { get; init; }
    public string? RiskLevel { get; init; }
    public string? Source { get; init; }
    public string? SaleType { get; init; }
    public bool? IsActive { get; init; }
    public bool? HasGarage { get; init; }
    public string? PebRating { get; init; }
    public DateTime? FirstSeenFrom { get; init; }

    /// <summary>Free-text search across title, description and address.</summary>
    public string? SearchText { get; init; }

    /// <summary>One of "price", "score", "livingarea"; defaults to first-seen date.</summary>
    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
