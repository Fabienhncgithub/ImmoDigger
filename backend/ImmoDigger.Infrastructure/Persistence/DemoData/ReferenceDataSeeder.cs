using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.DemoData;

/// <summary>
/// Seeds the baseline <see cref="ListingSource"/> rows for the five V1
/// sources (Biddit, Immoweb, Immovlan, Zimmo, and a generic real-estate
/// agency site) if they are not already present. Runs regardless of demo
/// mode: the collection framework (added in a later commit) needs these
/// rows to know which sources exist and whether they are enabled.
/// </summary>
public static class ReferenceDataSeeder
{
    public static async Task SeedAsync(ImmoDiggerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.ListingSources.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.ListingSources.AddRange(
            new ListingSource
            {
                Name = "Biddit",
                BaseUrl = "https://www.biddit.be",
                IsEnabled = true,
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                Name = "Immoweb",
                BaseUrl = "https://www.immoweb.be",
                IsEnabled = true,
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                Name = "Immovlan",
                BaseUrl = "https://www.immovlan.be",
                IsEnabled = true,
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                Name = "Zimmo",
                BaseUrl = "https://www.zimmo.be",
                IsEnabled = true,
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                // Placeholder generic agency source: disabled by default since
                // every agency site has a different structure. Enable and
                // point it at a specific agency once a collector exists for it.
                Name = "GenericAgency",
                BaseUrl = "https://example-agency.be",
                IsEnabled = false,
                PollingIntervalMinutes = 30,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
