using ImmoDigger.Application.Common;
using ImmoDigger.Application.DTOs;
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

    public async Task<PagedResult<PropertyListing>> GetPagedAsync(
        ListingQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PropertyListings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.City))
        {
            query = query.Where(l => l.City == parameters.City);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PostalCode))
        {
            query = query.Where(l => l.PostalCode == parameters.PostalCode);
        }

        if (parameters.MinimumPrice.HasValue)
        {
            query = query.Where(l => l.AskingPrice >= parameters.MinimumPrice);
        }

        if (parameters.MaximumPrice.HasValue)
        {
            query = query.Where(l => l.AskingPrice <= parameters.MaximumPrice);
        }

        if (parameters.MinimumUnits.HasValue)
        {
            query = query.Where(l =>
                (l.ObservedUnitCount ?? l.OfficialUnitCount ?? 0) >= parameters.MinimumUnits);
        }

        if (parameters.MinimumScore.HasValue)
        {
            query = query.Where(l => l.OpportunityScore >= parameters.MinimumScore);
        }

        if (!string.IsNullOrWhiteSpace(parameters.RiskLevel))
        {
            query = query.Where(l => l.RiskLevel == parameters.RiskLevel);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Source))
        {
            query = query.Where(l => l.Source == parameters.Source);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SaleType))
        {
            query = query.Where(l => l.SaleType == parameters.SaleType);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(l => l.IsActive == parameters.IsActive);
        }

        if (parameters.HasGarage.HasValue)
        {
            query = query.Where(l => l.HasGarage == parameters.HasGarage);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PebRating))
        {
            query = query.Where(l => l.PebRating == parameters.PebRating);
        }

        if (parameters.FirstSeenFrom.HasValue)
        {
            query = query.Where(l => l.FirstSeenAt >= parameters.FirstSeenFrom);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchText))
        {
            // .ToLower().Contains() rather than a provider-specific function
            // (e.g. Npgsql's EF.Functions.ILike): it translates on both
            // PostgreSQL and the InMemory provider used in tests.
            var text = parameters.SearchText.Trim().ToLowerInvariant();
            query = query.Where(l =>
                l.Title.ToLower().Contains(text) ||
                l.Description.ToLower().Contains(text) ||
                l.Address.ToLower().Contains(text));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySort(query, parameters.SortBy, parameters.SortDescending);

        var page = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PropertyListing>(items, totalCount, page, pageSize);
    }

    private static IQueryable<PropertyListing> ApplySort(
        IQueryable<PropertyListing> query, string? sortBy, bool descending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "price" => descending ? query.OrderByDescending(l => l.AskingPrice) : query.OrderBy(l => l.AskingPrice),
            "score" => descending ? query.OrderByDescending(l => l.OpportunityScore) : query.OrderBy(l => l.OpportunityScore),
            "livingarea" => descending ? query.OrderByDescending(l => l.LivingArea) : query.OrderBy(l => l.LivingArea),
            _ => descending ? query.OrderByDescending(l => l.FirstSeenAt) : query.OrderBy(l => l.FirstSeenAt),
        };

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
