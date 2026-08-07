namespace ImmoDigger.Application.DTOs;

/// <summary>Body of <c>POST /api/import/url</c>: a listing URL the user is looking at right now, plus an optional manual fallback if the page can't be fetched/parsed automatically.</summary>
public sealed record ImportUrlRequest
{
    public required string Url { get; init; }

    /// <summary>Filled in by the user when the page has no usable metadata (or couldn't be fetched at all).</summary>
    public ManualListingFields? ManualFallback { get; init; }
}

/// <summary>What the user pastes by hand for <see cref="ImportUrlRequest.ManualFallback"/> - mirrors the fields a listing page would normally expose as metadata.</summary>
public sealed record ManualListingFields
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Address { get; init; }

    public string? PostalCode { get; init; }

    public string? City { get; init; }

    public decimal? Price { get; init; }

    public string? PropertyType { get; init; }

    public string? ImageUrl { get; init; }
}

/// <summary>Result of a manual URL import attempt.</summary>
public sealed record ManualImportResult
{
    /// <summary>Set when enough data (fetched or manually supplied) was available to build a listing.</summary>
    public CollectedListing? Listing { get; init; }

    /// <summary>True when the page couldn't be fetched/understood and no <see cref="ImportUrlRequest.ManualFallback"/> was supplied - the caller should re-submit with one.</summary>
    public bool RequiresManualFallback { get; init; }
}
