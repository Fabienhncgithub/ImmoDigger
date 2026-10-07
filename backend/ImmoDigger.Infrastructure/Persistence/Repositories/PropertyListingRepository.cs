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

        if (parameters.Cities is { Length: > 0 })
        {
            query = query.Where(l => parameters.Cities.Contains(l.City));
        }

        if (parameters.PostalCodes is { Length: > 0 })
        {
            query = query.Where(l => parameters.PostalCodes.Contains(l.PostalCode));
        }

        if (parameters.PropertyTypes is { Length: > 0 })
        {
            query = query.Where(l => parameters.PropertyTypes.Contains(l.PropertyType));
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

        if (parameters.MinimumGrossYield.HasValue)
        {
            query = query.Where(l => l.EstimatedGrossYield >= parameters.MinimumGrossYield);
        }

        if (parameters.MinimumLivingArea.HasValue)
        {
            query = query.Where(l => l.LivingArea >= parameters.MinimumLivingArea);
        }

        if (parameters.IncludePublicSales == false)
        {
            query = query.Where(l => l.SaleType != "PublicSale");
        }

        if (!string.IsNullOrWhiteSpace(parameters.RiskLevel))
        {
            query = query.Where(l => l.RiskLevel == parameters.RiskLevel);
        }

        if (!string.IsNullOrWhiteSpace(parameters.UrbanisticStatus))
        {
            query = query.Where(l => l.UrbanisticStatus == parameters.UrbanisticStatus);
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

        if (parameters.ExcludeDemo == true)
        {
            query = query.Where(l => !l.ExternalId.StartsWith("DEMO-"));
        }

        if (parameters.HasGarage.HasValue)
        {
            query = query.Where(l => l.HasGarage == parameters.HasGarage);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PebRating))
        {
            query = query.Where(l => l.PebRating == parameters.PebRating);
        }

        if (parameters.MinimumAgeDays is > 0)
        {
            var seenBefore = DateTime.UtcNow.AddDays(-parameters.MinimumAgeDays.Value);
            query = query.Where(l => l.FirstSeenAt <= seenBefore);
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
        // Listings missing the sorted value always go last: PostgreSQL would
        // otherwise put NULLs first when sorting descending.
        sortBy?.ToLowerInvariant() switch
        {
            "price" => descending
                ? query.OrderBy(l => l.AskingPrice == null).ThenByDescending(l => l.AskingPrice)
                : query.OrderBy(l => l.AskingPrice == null).ThenBy(l => l.AskingPrice),
            "score" => descending
                ? query.OrderBy(l => l.OpportunityScore == null).ThenByDescending(l => l.OpportunityScore)
                : query.OrderBy(l => l.OpportunityScore == null).ThenBy(l => l.OpportunityScore),
            "livingarea" => descending
                ? query.OrderBy(l => l.LivingArea == null).ThenByDescending(l => l.LivingArea)
                : query.OrderBy(l => l.LivingArea == null).ThenBy(l => l.LivingArea),
            _ => descending ? query.OrderByDescending(l => l.FirstSeenAt) : query.OrderBy(l => l.FirstSeenAt),
        };

    // The three lookups below feed deduplication, which runs over a whole
    // batch of collected listings before anything is saved. They therefore
    // also look at listings added earlier in the same unit of work
    // (DbSet.Local): a database-only query can't see those yet, and the same
    // listing arriving twice in one batch (e.g. in several alert emails)
    // would be inserted once per occurrence.

    public async Task<PropertyListing?> GetBySourceAndExternalIdAsync(
        string source,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        var pending = dbContext.PropertyListings.Local
            .FirstOrDefault(l => l.Source == source && l.ExternalId == externalId);
        if (pending is not null)
        {
            return pending;
        }

        return await dbContext.PropertyListings
            // Loaded eagerly so PriceHistory reflects newly-appended entries
            // (via AddPriceHistoryEntry) immediately in memory, e.g. for a
            // caller that inspects the returned entity right away.
            .Include(l => l.PriceHistory)
            .FirstOrDefaultAsync(l => l.Source == source && l.ExternalId == externalId, cancellationToken);
    }

    public async Task<PropertyListing?> GetByNormalizedUrlAsync(
        string normalizedUrl,
        CancellationToken cancellationToken = default)
    {
        // UrlNormalizer's logic can't be translated to SQL, and PropertyListings.Url
        // stores the raw (non-normalized) URL, so the comparison has to happen
        // in memory. Acceptable at personal-app scale; if the table grows
        // large, consider persisting a precomputed normalized-URL column.
        await dbContext.PropertyListings
            .Include(l => l.PriceHistory)
            .LoadAsync(cancellationToken);

        return dbContext.PropertyListings.Local
            .FirstOrDefault(l => UrlNormalizer.Normalize(l.Url) == normalizedUrl);
    }

    public async Task<IReadOnlyList<PropertyListing>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.PropertyListings.LoadAsync(cancellationToken);

        return dbContext.PropertyListings.Local
            .OrderByDescending(l => l.FirstSeenAt)
            .ToList();
    }

    public async Task<IReadOnlyList<PropertyListing>> GetActiveBySourceAsync(
        string source,
        CancellationToken cancellationToken = default) =>
        await dbContext.PropertyListings
            .Where(l => l.Source == source && l.IsActive)
            .ToListAsync(cancellationToken);

    public Task<int> CountBySourceAsync(string source, CancellationToken cancellationToken = default) =>
        dbContext.PropertyListings.CountAsync(
            l => l.Source == source && !l.ExternalId.StartsWith("DEMO-"),
            cancellationToken);

    public async Task<DashboardStats> GetDashboardStatsAsync(
        decimal strongOpportunityThreshold,
        CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var activeListings = dbContext.PropertyListings.Where(l => l.IsActive);
        var realActiveListings = activeListings.Where(l => !l.ExternalId.StartsWith("DEMO-"));

        // Demo rows remain available for UI testing, but must never influence
        // decision-making KPIs shown to the investor.
        var newToday = await realActiveListings
            .CountAsync(l => l.FirstSeenAt >= todayUtc, cancellationToken);
        var activeCount = await activeListings.CountAsync(cancellationToken);
        var realActiveCount = await realActiveListings.CountAsync(cancellationToken);
        var demoActiveCount = await activeListings
            .CountAsync(l => l.ExternalId.StartsWith("DEMO-"), cancellationToken);
        var pricedActiveCount = await realActiveListings
            .CountAsync(l => l.AskingPrice.HasValue, cancellationToken);
        var scoredActiveCount = await realActiveListings
            .CountAsync(l => l.OpportunityScore.HasValue, cancellationToken);

        // Average() over a nullable column ignores nulls (matches SQL AVG()
        // semantics) and returns null rather than throwing on an empty set.
        var averagePrice = await realActiveListings
            .Select(l => l.AskingPrice)
            .AverageAsync(cancellationToken);
        var averageScore = await realActiveListings
            .Select(l => l.OpportunityScore)
            .AverageAsync(cancellationToken);
        var strongOpportunities = await realActiveListings
            .CountAsync(l => l.OpportunityScore >= strongOpportunityThreshold, cancellationToken);
        var highRisk = await realActiveListings
            .CountAsync(l => l.RiskLevel == RiskLevel.High, cancellationToken);

        return new DashboardStats(
            newToday,
            activeCount,
            realActiveCount,
            demoActiveCount,
            pricedActiveCount,
            scoredActiveCount,
            averagePrice,
            averageScore,
            strongOpportunities,
            highRisk);
    }

    public Task<int> CountMatchingProfileAsync(SearchProfile profile, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PropertyListings.Where(l => l.IsActive).AsQueryable();

        if (profile.PostalCodes.Length > 0)
        {
            query = query.Where(l => profile.PostalCodes.Contains(l.PostalCode));
        }

        if (profile.PropertyTypes.Length > 0)
        {
            query = query.Where(l => profile.PropertyTypes.Contains(l.PropertyType));
        }

        if (profile.MaximumPrice.HasValue)
        {
            query = query.Where(l => l.AskingPrice <= profile.MaximumPrice);
        }

        if (profile.MinimumGrossYield.HasValue)
        {
            query = query.Where(l => l.EstimatedGrossYield >= profile.MinimumGrossYield);
        }

        if (profile.MinimumUnitCount.HasValue)
        {
            query = query.Where(l =>
                (l.ObservedUnitCount ?? l.OfficialUnitCount ?? 0) >= profile.MinimumUnitCount);
        }

        if (profile.MinimumLivingArea.HasValue)
        {
            query = query.Where(l => l.LivingArea >= profile.MinimumLivingArea);
        }

        if (profile.RequireGarage)
        {
            query = query.Where(l => l.HasGarage == true);
        }

        if (!profile.IncludePublicSales)
        {
            query = query.Where(l => l.SaleType != "PublicSale");
        }

        if (profile.MinimumOpportunityScore.HasValue)
        {
            query = query.Where(l => l.OpportunityScore >= profile.MinimumOpportunityScore);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(PropertyListing listing, CancellationToken cancellationToken = default) =>
        await dbContext.PropertyListings.AddAsync(listing, cancellationToken);

    public void Update(PropertyListing listing) =>
        dbContext.PropertyListings.Update(listing);

    public void Remove(PropertyListing listing) =>
        dbContext.PropertyListings.Remove(listing);

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
