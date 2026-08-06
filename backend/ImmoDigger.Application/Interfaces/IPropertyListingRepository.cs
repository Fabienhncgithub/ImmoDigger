using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Persistence access for <see cref="PropertyListing"/>. Query methods
/// beyond simple lookups (pagination, filters, ...) are added in the
/// commit that introduces the listings API.
/// </summary>
public interface IPropertyListingRepository
{
    Task<PropertyListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deduplication level 1: exact (Source, ExternalId) match.</summary>
    Task<PropertyListing?> GetBySourceAndExternalIdAsync(
        string source,
        string externalId,
        CancellationToken cancellationToken = default);

    /// <summary>Deduplication level 2: exact normalized URL match.</summary>
    Task<PropertyListing?> GetByNormalizedUrlAsync(
        string normalizedUrl,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyListing>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyListing>> GetActiveBySourceAsync(
        string source,
        CancellationToken cancellationToken = default);

    Task AddAsync(PropertyListing listing, CancellationToken cancellationToken = default);

    void Update(PropertyListing listing);

    /// <summary>
    /// Appends a new price point to an already-tracked <paramref name="listing"/>
    /// (e.g. when a collected price differs from the last known one).
    /// Deliberately explicit rather than just `listing.PriceHistory.Add(entry)`:
    /// EF Core cannot reliably infer that a new child added to an
    /// already-persisted parent's loaded collection is itself new when its
    /// key (a client-generated GUID) is already non-default, so the entry
    /// must be registered on the tracker directly.
    /// </summary>
    void AddPriceHistoryEntry(PropertyListing listing, ListingPriceHistory entry);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
