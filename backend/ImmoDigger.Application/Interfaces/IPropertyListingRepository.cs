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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
