using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.DemoData;

/// <summary>
/// Seeds the baseline <see cref="ListingSource"/> rows if none exist yet:
/// the five V1 sources (Biddit, Immoweb, Immovlan, Zimmo, and a generic
/// real-estate agency site), plus a handful of Belgian institutional
/// sellers that periodically auction off surplus real estate (bpost,
/// the federal Régie des Bâtiments - which also handles former Défense/
/// army sites, Proximus, the Flemish region, and the SNCB/Infrabel
/// railway). All institutional sources are seeded disabled: their URLs
/// are verified real endpoints, but no collector has been vetted or
/// implemented for them yet (same rule as the rest of the project - no
/// scraping without first checking for an API/RSS feed and reading the
/// terms of use). Runs regardless of demo mode: the collection framework
/// needs these rows to know which sources exist and whether they are
/// enabled.
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
            },
            new ListingSource
            {
                // bpost periodically sells surplus post-office buildings.
                Name = "BpostImmo",
                BaseUrl = "https://bpostimmo.be/fr/a-vendre/",
                IsEnabled = false,
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // Federal state property manager; also handles the sale of
                // former Défense (army) sites such as barracks and domains.
                Name = "RegieDesBatiments",
                BaseUrl = "https://www.regiedesbatiments.be/fr/venteslocations",
                IsEnabled = false,
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // Proximus is selling off >500 former telecom-exchange
                // buildings through 2035 as it retires its copper network.
                Name = "ProximusRealEstate",
                BaseUrl = "https://proximusforrealestate.be/fr/",
                IsEnabled = false,
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // Flemish region real estate sales/auctions (Vlaamse
                // overheid - Facilitair Bedrijf, Vastgoedtransacties).
                Name = "VlaamseOverheidVastgoed",
                BaseUrl = "https://vastgoedtransacties.be/",
                IsEnabled = false,
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // SNCB (Belgian railway) surplus buildings and land.
                Name = "SncbImmo",
                BaseUrl = "https://www.belgiantrain.be/fr/3rd-party-services/3rd-party-sales/immo/annonces_ventes",
                IsEnabled = false,
                PollingIntervalMinutes = 60,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
