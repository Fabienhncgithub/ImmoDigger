using ImmoDigger.Domain.Common;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.DemoData;

/// <summary>
/// Seeds the baseline <see cref="ListingSource"/> rows if none exist yet.
/// Every row's <see cref="ListingSource.CollectionMethod"/> and
/// <see cref="ListingSource.Allowed"/> record the actual compliance
/// decision for that source - see each row's <see cref="ListingSource.Notes"/>
/// for what was checked and why. <see cref="ListingSource.IsEnabled"/> only
/// controls scheduling on top of that; it never overrides <c>Allowed</c>
/// (enforced centrally in <c>ListingCollectionBackgroundService</c>).
///
/// Immoweb, Immovlan and Zimmo are <see cref="Domain.Common.CollectionMethod.Email"/>
/// sources: never scraped directly (explicit ToS prohibition for Immoweb,
/// active WAF/Cloudflare bot-blocking for the other two), but their alert
/// emails - already delivered to the user's own inbox - are fair game and
/// parsed by the "EmailImport" collector below. Runs regardless of demo
/// mode: the collection framework needs these rows to know which sources
/// exist and whether they are enabled.
/// </summary>
public static class ReferenceDataSeeder
{
    public static async Task SeedAsync(ImmoDiggerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.ListingSources.AnyAsync(cancellationToken))
        {
            return;
        }

        var checkedAt = new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc);

        dbContext.ListingSources.AddRange(
            new ListingSource
            {
                Name = "Biddit",
                BaseUrl = "https://www.biddit.be",
                IsEnabled = true,
                CollectionMethod = CollectionMethod.Api,
                Allowed = true,
                RobotsCheckedAt = checkedAt,
                Notes = "Discovery via the published sitemap; listing detail via the public unauthenticated JSON endpoint the site's own app calls. No anti-bot protection found.",
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                // Not scraped directly (explicit anti-scraping clause with
                // penalties in the terms of use). Alert emails are parsed
                // instead - see EmailImport below and ImmowebEmailParser.
                Name = "Immoweb",
                BaseUrl = "https://www.immoweb.be",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Email,
                Allowed = false,
                TermsCheckedAt = checkedAt,
                Notes = "ExternalAlertSource: terms of use explicitly prohibit automated collection (penalty clauses up to EUR 500,000 / 2 years). Website scraping stays Allowed=false permanently; alert-email parsing is the only sanctioned path (see ImmowebEmailParser).",
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                // Not scraped directly (active WAF blocking automated
                // requests). Alert emails are parsed instead.
                Name = "Immovlan",
                BaseUrl = "https://www.immovlan.be",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Email,
                Allowed = false,
                RobotsCheckedAt = checkedAt,
                Notes = "ExternalAlertSource: active WAF blocks automated requests at the network level. Website scraping stays Allowed=false permanently; alert-email parsing is the only sanctioned path (see ImmovlanEmailParser).",
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                // Not scraped directly (Cloudflare bot-fingerprinting).
                // Alert emails are parsed instead.
                Name = "Zimmo",
                BaseUrl = "https://www.zimmo.be",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Email,
                Allowed = false,
                RobotsCheckedAt = checkedAt,
                Notes = "ExternalAlertSource: Cloudflare bot-fingerprinting on every page. Website scraping stays Allowed=false permanently; alert-email parsing is the only sanctioned path (see ZimmoEmailParser).",
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                // Umbrella source actually run by ListingCollectionBackgroundService:
                // EmailImportListingCollector fans out to every registered
                // IEmailListingParser (Immoweb/Immovlan/Zimmo/agencies) in
                // one pass. Disabled until a real IEmailInbox replaces the
                // NullEmailInbox placeholder - see its doc comment.
                Name = "EmailImport",
                BaseUrl = "mailbox://alerts",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Email,
                Allowed = true,
                Notes = "Parses alert emails already in the user's own inbox for Immoweb/Immovlan/Zimmo/agencies. Enable once a real IEmailInbox (IMAP or forwarding webhook) is configured; NullEmailInbox is a no-op until then.",
                PollingIntervalMinutes = 30,
            },
            new ListingSource
            {
                // Placeholder generic agency source: disabled by default since
                // every agency site has a different structure. Enable and
                // point it at a specific agency once a collector exists for it.
                Name = "GenericAgency",
                BaseUrl = "https://example-agency.be",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Disabled,
                Allowed = false,
                Notes = "Not vetted for any specific agency yet. Per-agency onboarding checklist: public API? RSS/feed? email alerts? then robots.txt, then terms of use, only then (maybe) HTML.",
                PollingIntervalMinutes = 30,
            },
            new ListingSource
            {
                // bpost periodically sells surplus post-office buildings.
                Name = "BpostImmo",
                BaseUrl = "https://bpostimmo.be/fr/a-vendre/",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Disabled,
                Allowed = false,
                RobotsCheckedAt = checkedAt,
                Notes = "robots.txt permissive, no anti-bot protection found, but listing detail needs the same JS-bundle reverse-engineering effort as Biddit - not done yet. Legitimate future candidate.",
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // Federal state property manager; also handles the sale of
                // former Défense (army) sites such as barracks and domains.
                Name = "RegieDesBatiments",
                BaseUrl = "https://www.regiedesbatiments.be/fr/venteslocations",
                IsEnabled = true,
                CollectionMethod = CollectionMethod.Html,
                Allowed = true,
                RobotsCheckedAt = checkedAt,
                Notes = "Clean server-rendered HTML, no anti-bot protection, permissive robots.txt. Also the sales channel for former army/Défense sites, so a single collector covers both.",
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // Proximus is selling off >500 former telecom-exchange
                // buildings through 2035 as it retires its copper network.
                Name = "ProximusRealEstate",
                BaseUrl = "https://proximusforrealestate.be/fr/",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Disabled,
                Allowed = false,
                Notes = "URL verified real; no collector vetted/implemented yet.",
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // Flemish region real estate sales/auctions (Vlaamse
                // overheid - Facilitair Bedrijf, Vastgoedtransacties).
                Name = "VlaamseOverheidVastgoed",
                BaseUrl = "https://vastgoedtransacties.be/",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Disabled,
                Allowed = false,
                RobotsCheckedAt = checkedAt,
                Notes = "robots.txt permissive, no anti-bot protection, but markup is Elementor-based and too structurally inconsistent to parse reliably without more page-sampling. Legitimate future candidate.",
                PollingIntervalMinutes = 60,
            },
            new ListingSource
            {
                // SNCB (Belgian railway) surplus buildings and land.
                Name = "SncbImmo",
                BaseUrl = "https://www.belgiantrain.be/fr/3rd-party-services/3rd-party-sales/immo/annonces_ventes",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Disabled,
                Allowed = false,
                RobotsCheckedAt = checkedAt,
                Notes = "Cloudflare bot-fingerprinting on belgiantrain.be. Allowed=false permanently unless that changes.",
                PollingIntervalMinutes = 60,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
