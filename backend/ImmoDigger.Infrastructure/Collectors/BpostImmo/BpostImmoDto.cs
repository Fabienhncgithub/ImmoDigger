namespace ImmoDigger.Infrastructure.Collectors.BpostImmo;

// Mirrors the subset of the JSON that bpostimmo.be's listing pages embed
// inline via `app.set("estateGroups", [...])` (a Zabun-platform real
// estate site) - reverse-engineered by reading that page's own HTML, not
// from official documentation. Only the fields this collector actually
// uses are modeled; everything else in the real payload is ignored by
// System.Text.Json automatically.

public sealed class BpostEstateGroupDto
{
    public List<BpostEstateWrapperDto>? Estates { get; set; }
}

public sealed class BpostEstateWrapperDto
{
    public string? Id { get; set; }

    /// <summary>Relative path, e.g. "/fr/offre/4041602/...". Combine with the site's base URL.</summary>
    public string? Uri { get; set; }

    public BpostStatusDto? Status { get; set; }

    public BpostEstateDto? Estate { get; set; }
}

public sealed class BpostStatusDto
{
    public BpostLocalizedTextDto? Name { get; set; }
}

public sealed class BpostEstateDto
{
    public BpostGeneralDto? General { get; set; }

    public BpostDimensionsDto? Dimensions { get; set; }

    public BpostEnergyDto? Energy { get; set; }

    public List<BpostPictureDto>? Pictures { get; set; }
}

public sealed class BpostGeneralDto
{
    public BpostLocalizedTextDto? Title { get; set; }

    public BpostLocalizedTextDto? Description { get; set; }

    public BpostLocalizedTextDto? SubType { get; set; }

    public BpostPriceDto? Price { get; set; }

    public BpostAddressDto? Address { get; set; }
}

public sealed class BpostPriceDto
{
    public decimal? Value { get; set; }
}

public sealed class BpostAddressDto
{
    public BpostLocalizedTextDto? Street { get; set; }

    public string? Number { get; set; }

    public BpostCityDto? City { get; set; }
}

public sealed class BpostCityDto
{
    public string? Zip { get; set; }

    public string? Name { get; set; }
}

public sealed class BpostDimensionsDto
{
    public decimal? AreaGround { get; set; }

    public decimal? AreaBuild { get; set; }
}

public sealed class BpostEnergyDto
{
    /// <summary>e.g. "epc_f" - lowercased, prefixed. See ExtractPebLetter.</summary>
    public string? EnergyLabel { get; set; }
}

public sealed class BpostPictureDto
{
    public string? File { get; set; }

    public string? Thumbnail { get; set; }
}

/// <summary>Some string fields on this platform are per-language ({"fr": "...", "nl": "..."}); others (oddly, the wrapper's own top-level "title") are a bare string. Both shapes are handled where relevant by the collector, not this DTO.</summary>
public sealed class BpostLocalizedTextDto
{
    public string? Fr { get; set; }

    public string? Nl { get; set; }
}
