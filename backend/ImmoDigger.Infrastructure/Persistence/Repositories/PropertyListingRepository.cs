using ImmoDigger.Application.Interfaces;
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
            .FirstOrDefaultAsync(l => l.Source == source && l.ExternalId == externalId, cancellationToken);

    public Task<PropertyListing?> GetByNormalizedUrlAsync(
        string normalizedUrl,
        CancellationToken cancellationToken = default) =>
        dbContext.PropertyListings
            .FirstOrDefaultAsync(l => l.Url == normalizedUrl, cancellationToken);

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

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
