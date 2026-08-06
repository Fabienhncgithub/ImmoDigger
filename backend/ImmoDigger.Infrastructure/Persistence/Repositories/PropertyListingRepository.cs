using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Common;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.Repositories;

public class PropertyListingRepository(ImmoDiggerDbContext dbContext) : IPropertyListingRepository
{
    public Task<PropertyListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.PropertyListings
            .Include(l => l.PriceHistory)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<PropertyListing?> GetBySourceAndExternalIdAsync(
        string source,
        string externalId,
        CancellationToken cancellationToken = default) =>
        dbContext.PropertyListings
            // Loaded eagerly so PriceHistory reflects newly-appended entries
            // (via AddPriceHistoryEntry) immediately in memory, e.g. for a
            // caller that inspects the returned entity right away.
            .Include(l => l.PriceHistory)
            .FirstOrDefaultAsync(l => l.Source == source && l.ExternalId == externalId, cancellationToken);

    public async Task<PropertyListing?> GetByNormalizedUrlAsync(
        string normalizedUrl,
        CancellationToken cancellationToken = default)
    {
        // UrlNormalizer's logic can't be translated to SQL, and PropertyListings.Url
        // stores the raw (non-normalized) URL, so the comparison has to happen
        // in memory. Acceptable at personal-app scale; if the table grows
        // large, consider persisting a precomputed normalized-URL column.
        var listings = await dbContext.PropertyListings
            .Include(l => l.PriceHistory)
            .ToListAsync(cancellationToken);

        return listings.FirstOrDefault(l => UrlNormalizer.Normalize(l.Url) == normalizedUrl);
    }

    public async Task<IReadOnlyList<PropertyListing>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PropertyListings
            .OrderByDescending(l => l.FirstSeenAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PropertyListing>> GetActiveBySourceAsync(
        string source,
        CancellationToken cancellationToken = default) =>
        await dbContext.PropertyListings
            .Where(l => l.Source == source && l.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PropertyListing listing, CancellationToken cancellationToken = default) =>
        await dbContext.PropertyListings.AddAsync(listing, cancellationToken);

    public void Update(PropertyListing listing) =>
        dbContext.PropertyListings.Update(listing);

    public void AddPriceHistoryEntry(PropertyListing listing, ListingPriceHistory entry)
    {
        entry.PropertyListingId = listing.Id;
        listing.PriceHistory.Add(entry);

        // Registered on the DbSet explicitly rather than relying on EF Core
        // to infer this from the collection mutation above: when the parent
        // is already tracked (not itself being Added), EF cannot reliably
        // tell a brand-new child apart from an existing one once its key
        // (a client-generated GUID) is already non-default - it would mark
        // it Modified instead of Added and SaveChanges would then fail with
        // "entity does not exist in the store".
        dbContext.ListingPriceHistories.Add(entry);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
