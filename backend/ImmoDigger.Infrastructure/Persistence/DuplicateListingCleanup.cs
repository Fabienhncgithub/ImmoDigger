using ImmoDigger.Application.Services;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence;

/// <summary>
/// Merges listings that deduplication should have caught: several rows for
/// one Source + ExternalId (created when the same listing came in more than
/// once within a single collection batch), and the same property advertised
/// on two portals (see <see cref="ListingDeduplicationService.IsSameProperty"/>).
/// Idempotent, so it simply runs at every startup.
/// </summary>
public static class DuplicateListingCleanup
{
    public static async Task<int> RunAsync(ImmoDiggerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var listings = await dbContext.PropertyListings.ToListAsync(cancellationToken);
        var removed = new HashSet<PropertyListing>();

        foreach (var group in listings.GroupBy(l => (l.Source, l.ExternalId)))
        {
            Merge(group.ToList(), removed);
        }

        // Fictional demo listings are left alone: they are seeded on purpose.
        var crossPortalCandidates = listings
            .Where(l => !removed.Contains(l) && !l.ExternalId.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase))
            .Where(l => !string.IsNullOrWhiteSpace(l.PostalCode) && l.AskingPrice is > 0 && l.LivingArea is > 0)
            .GroupBy(l => (PostalCode: l.PostalCode.Trim().ToUpperInvariant(), l.AskingPrice, l.LivingArea));

        foreach (var group in crossPortalCandidates)
        {
            // One row per portal at this point; identical rows from a single
            // portal are distinct units and stay.
            if (group.Select(l => l.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count() == group.Count())
            {
                Merge(group.ToList(), removed);
            }
        }

        if (removed.Count > 0)
        {
            dbContext.PropertyListings.RemoveRange(removed);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return removed.Count;
    }

    private static void Merge(List<PropertyListing> duplicates, HashSet<PropertyListing> removed)
    {
        if (duplicates.Count < 2)
        {
            return;
        }

        // Whatever the user typed in wins; otherwise the oldest sighting.
        var kept = duplicates
            .OrderByDescending(HasUserInput)
            .ThenBy(l => l.FirstSeenAt)
            .First();

        kept.FirstSeenAt = duplicates.Min(l => l.FirstSeenAt);
        kept.LastSeenAt = duplicates.Max(l => l.LastSeenAt);
        kept.PublishedAt = duplicates.Min(l => l.PublishedAt) ?? kept.PublishedAt;

        foreach (var duplicate in duplicates.Where(l => l != kept))
        {
            removed.Add(duplicate);
        }
    }

    private static bool HasUserInput(PropertyListing listing) =>
        listing.IsReviewed ||
        !string.IsNullOrWhiteSpace(listing.PersonalNotes) ||
        listing.EstimatedMonthlyRentPerUnit.HasValue ||
        listing.EstimatedAcquisitionCosts.HasValue ||
        listing.EstimatedRenovationBudget.HasValue;
}
