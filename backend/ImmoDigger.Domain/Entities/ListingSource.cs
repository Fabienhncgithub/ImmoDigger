using ImmoDigger.Domain.Common;

namespace ImmoDigger.Domain.Entities;

/// <summary>
/// Configuration and run status of a collectible source (Biddit, an email
/// alert stream, an institutional seller, a generic agency site, ...).
/// Drives whether a listing collector runs and how often.
///
/// <see cref="CollectionMethod"/> and <see cref="Allowed"/> are the actual
/// compliance gate: <see cref="IsEnabled"/> only says "run on schedule", it
/// never overrides them. A collector implementation must check
/// <see cref="Allowed"/> (or simply not exist) for any source whose method
/// would otherwise let it touch a site directly - see the notes on each
/// seeded row in <c>ReferenceDataSeeder</c> for why a given source is or
/// isn't allowed.
/// </summary>
public class ListingSource
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Matches the collector's source name and <see cref="PropertyListing.Source"/>.</summary>
    public required string Name { get; set; }

    public required string BaseUrl { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>How this source is collected. See <see cref="CollectionMethod"/>.</summary>
    public CollectionMethod CollectionMethod { get; set; } = CollectionMethod.Disabled;

    /// <summary>
    /// Hard compliance gate, independent of <see cref="IsEnabled"/>. When
    /// false, no collector may run for this source regardless of method -
    /// this is what a source like Immoweb, Immovlan or Zimmo is set to for
    /// any method other than <see cref="Domain.Common.CollectionMethod.Email"/>
    /// (their alert emails are fine to parse; scraping their site is not).
    /// </summary>
    public bool Allowed { get; set; } = true;

    /// <summary>When the terms of use were last read for this source, if applicable.</summary>
    public DateTime? TermsCheckedAt { get; set; }

    /// <summary>When robots.txt was last checked for this source, if applicable.</summary>
    public DateTime? RobotsCheckedAt { get; set; }

    /// <summary>Free-text explanation of the compliance decision (what was checked, what was found, why the method/Allowed values are what they are).</summary>
    public string? Notes { get; set; }

    public int PollingIntervalMinutes { get; set; } = 15;

    public DateTime? LastSuccessfulRunAt { get; set; }

    public DateTime? LastFailedRunAt { get; set; }

    public string? LastError { get; set; }
}
