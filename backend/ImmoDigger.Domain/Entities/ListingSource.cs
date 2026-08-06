namespace ImmoDigger.Domain.Entities;

/// <summary>
/// Configuration and run status of a collectible source (Biddit, Immoweb,
/// Immovlan, Zimmo, a generic agency site, ...). Drives whether a listing
/// collector runs and how often (collectors are added in a later commit).
/// </summary>
public class ListingSource
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Matches the collector's source name and <see cref="PropertyListing.Source"/>.</summary>
    public required string Name { get; set; }

    public required string BaseUrl { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int PollingIntervalMinutes { get; set; } = 15;

    public DateTime? LastSuccessfulRunAt { get; set; }

    public DateTime? LastFailedRunAt { get; set; }

    public string? LastError { get; set; }
}
