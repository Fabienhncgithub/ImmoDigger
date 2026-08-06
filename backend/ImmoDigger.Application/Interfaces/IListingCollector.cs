using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Collects listings from a single source (a public API, an RSS feed, or
/// HTML parsing when nothing more structured is available). Implementations
/// must never bypass a CAPTCHA, an authentication wall, a rate limit, or an
/// explicit prohibition; use a reasonable polling frequency and a clear
/// User-Agent when allowed.
///
/// Errors must be handled internally where possible; <see cref="CollectAsync"/>
/// is allowed to throw (the collection framework catches it, records it on
/// the corresponding <see cref="Domain.Entities.ListingSource"/>, and keeps
/// running the other sources), but implementations should avoid throwing
/// for expected/recoverable conditions (e.g. an empty result page).
/// </summary>
public interface IListingCollector
{
    /// <summary>Must match a <see cref="Domain.Entities.ListingSource.Name"/> row.</summary>
    string SourceName { get; }

    Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken);
}
