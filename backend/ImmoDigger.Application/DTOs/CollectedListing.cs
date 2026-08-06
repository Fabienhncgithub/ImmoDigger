namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Raw data produced by an <see cref="Interfaces.IListingCollector"/> for a
/// single listing, before deduplication against existing
/// <see cref="Domain.Entities.PropertyListing"/> rows (handled by the
/// deduplication service added in a later commit). Mirrors the scrapeable
/// subset of <see cref="Domain.Entities.PropertyListing"/>: it excludes
/// fields that only exist once a listing has been persisted and analyzed
/// (Id, FirstSeenAt/LastSeenAt, OpportunityScore, RiskLevel, personal
/// notes, manual investment inputs, ...).
/// </summary>
public sealed record CollectedListing
{
    public required string Source { get; init; }

    public required string ExternalId { get; init; }

    public required string Url { get; init; }

    public required string Title { get; init; }

    public string? ImageUrl { get; init; }

    public string Description { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public decimal? AskingPrice { get; init; }

    public decimal? CurrentBid { get; init; }

    public required string SaleType { get; init; }

    public required string PropertyType { get; init; }

    public int? BedroomCount { get; init; }

    public int? BathroomCount { get; init; }

    public int? OfficialUnitCount { get; init; }

    public int? ObservedUnitCount { get; init; }

    public decimal? LivingArea { get; init; }

    public decimal? LandArea { get; init; }

    public string? PebRating { get; init; }

    public decimal? PebConsumption { get; init; }

    public bool? ElectricalInstallationCompliant { get; init; }

    public bool? IsOccupied { get; init; }

    public bool? HasGarage { get; init; }

    public bool? HasTerrace { get; init; }

    public bool? HasGarden { get; init; }

    public decimal? CadastralIncome { get; init; }

    public DateTime? AuctionStartDate { get; init; }

    public DateTime? AuctionEndDate { get; init; }

    public DateTime? PublishedAt { get; init; }

    /// <summary>Hash of the cleaned/normalized content, used for deduplication level 5.</summary>
    public required string RawContentHash { get; init; }
}
