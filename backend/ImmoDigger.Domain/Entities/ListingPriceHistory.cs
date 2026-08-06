namespace ImmoDigger.Domain.Entities;

/// <summary>
/// One recorded price point for a <see cref="PropertyListing"/>. A new row
/// is appended whenever a collector observes a price different from the
/// last known one, so the full price trajectory of a listing can be
/// displayed and price-drop notifications can be triggered.
/// </summary>
public class ListingPriceHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PropertyListingId { get; set; }

    public PropertyListing? PropertyListing { get; set; }

    public decimal Price { get; set; }

    public DateTime RecordedAt { get; set; }
}
