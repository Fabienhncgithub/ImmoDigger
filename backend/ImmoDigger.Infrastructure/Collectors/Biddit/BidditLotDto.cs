using System.Text.Json.Serialization;

namespace ImmoDigger.Infrastructure.Collectors.Biddit;

/// <summary>
/// Shape of the JSON returned by Biddit's own (undocumented, but public
/// and unauthenticated) "/api/eco/biddit-bff/lot/{reference}" endpoint.
/// Only the fields ImmoDigger actually uses are modeled; the real
/// response carries many more (bidding mechanics, attachments, heritage/
/// expropriation flags, ...).
/// </summary>
public sealed class BidditLotDto
{
    public string? Reference { get; set; }

    public string? HandlingMethod { get; set; }

    [JsonPropertyName("firstPublicationDateTime")]
    public DateTime? FirstPublicationDateTime { get; set; }

    public DateTime? BiddingStartDateTime { get; set; }

    public DateTime? BiddingEndDateTime { get; set; }

    /// <summary>Live/last bid on an online public sale.</summary>
    public decimal? CurrentPrice { get; set; }

    /// <summary>"Mise a prix" on a public sale.</summary>
    public decimal? StartingPrice { get; set; }

    /// <summary>Asking price on a private ("make an offer from") sale.</summary>
    public decimal? SellingPrice { get; set; }

    public List<BidditPropertyDto> Properties { get; set; } = [];
}

public sealed class BidditPropertyDto
{
    public string? Reference { get; set; }

    public string? PropertyType { get; set; }

    public string? PropertySubtype { get; set; }

    public BidditLocalizedTextDto? Title { get; set; }

    public BidditLocalizedTextDto? Description { get; set; }

    public BidditAddressDto? Address { get; set; }

    public List<BidditPictureDto> Pictures { get; set; } = [];

    public decimal? LivingSurfaceArea { get; set; }

    public int? NumberOfBedrooms { get; set; }

    public int? NumberOfBathrooms { get; set; }

    public string? EnergeticClassRbc { get; set; }

    public string? EnergeticClassRw { get; set; }

    public string? EnergeticClassRf { get; set; }

    public BidditConstructionDto? Construction { get; set; }

    public BidditFeaturesDto? Features { get; set; }

    public BidditLandIncomeDto? LandIncome { get; set; }
}

public sealed class BidditLocalizedTextDto
{
    public string? Fr { get; set; }
    public string? Nl { get; set; }
    public string? En { get; set; }
}

public sealed class BidditAddressDto
{
    public string? EstateNumber { get; set; }
    public string? PostalCode { get; set; }
    public BidditLocalizedTextDto? Street { get; set; }
    public BidditLocalizedTextDto? Municipality { get; set; }
}

public sealed class BidditPictureDto
{
    public int? OrderIndex { get; set; }
    public string? Medium { get; set; }
    public string? Large { get; set; }
}

public sealed class BidditConstructionDto
{
    public int? NumberOfHousingUnits { get; set; }
    public int? ConstructionYear { get; set; }
}

public sealed class BidditFeaturesDto
{
    public decimal? TerrainSurface { get; set; }
    public decimal? GarageSurface { get; set; }
    public bool? HasTerrace { get; set; }
    public bool? HasGarden { get; set; }
}

public sealed class BidditLandIncomeDto
{
    public decimal? LandIncome { get; set; }
}
