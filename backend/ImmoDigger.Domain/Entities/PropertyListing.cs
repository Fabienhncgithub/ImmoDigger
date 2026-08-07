namespace ImmoDigger.Domain.Entities;

/// <summary>
/// A real-estate listing collected from a source (Biddit, Immoweb,
/// Immovlan, Zimmo, an agency website, ...). This is the central entity
/// of ImmoDigger: every collector produces data that ends up shaping one
/// <see cref="PropertyListing"/> row, deduplicated against existing ones.
/// </summary>
public class PropertyListing
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Name of the source that produced this listing (matches <see cref="ListingSource.Name"/>).</summary>
    public required string Source { get; set; }

    /// <summary>Identifier of the listing on the source's own system. Used for deduplication level 1.</summary>
    public required string ExternalId { get; set; }

    public required string Url { get; set; }

    public required string Title { get; set; }

    /// <summary>URL of the listing's main/cover photo, if the source provides one. Not in the original field list - added for the listing cards/detail page.</summary>
    public string? ImageUrl { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public decimal? AskingPrice { get; set; }

    public decimal? CurrentBid { get; set; }

    public decimal? EstimatedFinalPrice { get; set; }

    /// <summary>e.g. "RegularSale", "PublicSale". Kept as free text: each source has its own vocabulary.</summary>
    public required string SaleType { get; set; }

    /// <summary>e.g. "IncomeBuilding", "ApartmentBuilding", "House". Free text for the same reason as <see cref="SaleType"/>.</summary>
    public required string PropertyType { get; set; }

    public int? BedroomCount { get; set; }

    public int? BathroomCount { get; set; }

    /// <summary>Number of units the source officially declares (e.g. cadastral records, listing metadata).</summary>
    public int? OfficialUnitCount { get; set; }

    /// <summary>Number of units inferred from the free-text description (kitchens, meters, ...). See risk analysis.</summary>
    public int? ObservedUnitCount { get; set; }

    public decimal? LivingArea { get; set; }

    public decimal? LandArea { get; set; }

    /// <summary>Belgian PEB / EPC rating: A, B, C, D, E, F or G.</summary>
    public string? PebRating { get; set; }

    public decimal? PebConsumption { get; set; }

    public bool? ElectricalInstallationCompliant { get; set; }

    public bool? IsOccupied { get; set; }

    public bool? HasGarage { get; set; }

    public bool? HasTerrace { get; set; }

    public bool? HasGarden { get; set; }

    public decimal? CadastralIncome { get; set; }

    // --- Email-import provenance (added for the compliant multi-source
    // pipeline: Immoweb/Immovlan/Zimmo/agency listings only ever reach
    // ImmoDigger via alert emails the user already receives, never by
    // scraping those sites directly). Null for listings collected any
    // other way. -------------------------------------------------------

    /// <summary>Message-Id of the alert email this listing was extracted from, if any.</summary>
    public string? EmailMessageId { get; set; }

    public string? EmailSubject { get; set; }

    /// <summary>Sender address of the alert email, if any (e.g. alerts@immoweb.be).</summary>
    public string? EmailSender { get; set; }

    public DateTime? AuctionStartDate { get; set; }

    public DateTime? AuctionEndDate { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Hash of the cleaned listing content, used for deduplication level 5 (content comparison).</summary>
    public required string RawContentHash { get; set; }

    public decimal? OpportunityScore { get; set; }

    public decimal? EstimatedGrossYield { get; set; }

    public decimal? EstimatedRenovationCost { get; set; }

    /// <summary>"Low", "Medium" or "High". See <see cref="ImmoDigger.Domain.Common.RiskLevel"/>.</summary>
    public string? RiskLevel { get; set; }

    public string? RiskSummary { get; set; }

    // --- Manual investment inputs (V1) ---------------------------------
    // The user fills these in manually on the listing detail page; a later
    // version may estimate them automatically. They feed
    // IInvestmentAnalysisService's gross yield calculation.

    public decimal? EstimatedMonthlyRentPerUnit { get; set; }

    public decimal? EstimatedAcquisitionCosts { get; set; }

    public decimal? EstimatedRenovationBudget { get; set; }

    // --- Personal review tracking ---------------------------------------

    public string? PersonalNotes { get; set; }

    public bool IsReviewed { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public ICollection<ListingPriceHistory> PriceHistory { get; set; } = new List<ListingPriceHistory>();

    public ICollection<NotificationHistory> Notifications { get; set; } = new List<NotificationHistory>();
}
