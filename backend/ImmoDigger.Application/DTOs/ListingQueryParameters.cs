namespace ImmoDigger.Application.DTOs;

/// <summary>Filters, sort and pagination for <c>GET /api/listings</c>.</summary>
public sealed record ListingQueryParameters
{
    /// <summary>
    /// Matches any of the given postal codes (OR). Plural rather than the
    /// single "postalCode" in the original filter list - added so a user
    /// can search several communes at once, the way real-estate portals do.
    /// </summary>
    public string[]? PostalCodes { get; init; }

    /// <summary>Matches any of the given cities (OR). Same rationale as <see cref="PostalCodes"/>.</summary>
    public string[]? Cities { get; init; }

    /// <summary>
    /// Matches any of the given property types (e.g. "IncomeBuilding",
    /// "Warehouse"). Not in the original filter list, but needed:
    /// without it there is no way to search specifically for immeubles/
    /// maisons de rapport.
    /// </summary>
    public string[]? PropertyTypes { get; init; }

    public decimal? MinimumPrice { get; init; }
    public decimal? MaximumPrice { get; init; }
    public int? MinimumUnits { get; init; }
    public decimal? MinimumScore { get; init; }
    public decimal? MinimumGrossYield { get; init; }
    public decimal? MinimumLivingArea { get; init; }

    /// <summary>
    /// When explicitly false, public/auction sales are excluded. Null (the
    /// default) keeps both regular and public sales.
    /// </summary>
    public bool? IncludePublicSales { get; init; }

    public string? RiskLevel { get; init; }

    /// <summary>"Infraction", "Compliant" or "Unknown": what the listing text says about its planning situation.</summary>
    public string? UrbanisticStatus { get; init; }

    public string? Source { get; init; }
    public string? SaleType { get; init; }
    public bool? IsActive { get; init; }

    /// <summary>When true, fictional demo listings are left out, as in the dashboard KPIs.</summary>
    public bool? ExcludeDemo { get; init; }

    public bool? HasGarage { get; init; }
    public string? PebRating { get; init; }
    public DateTime? FirstSeenFrom { get; init; }

    /// <summary>Keeps only listings first seen at least this many days ago ("on the market for a while").</summary>
    public int? MinimumAgeDays { get; init; }

    /// <summary>Free-text search across title, description and address.</summary>
    public string? SearchText { get; init; }

    /// <summary>One of "price", "score", "livingarea"; defaults to first-seen date.</summary>
    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
