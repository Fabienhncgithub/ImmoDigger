using ImmoDigger.Domain.Common;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.DemoData;

/// <summary>
/// Seeds any baseline <see cref="ListingSource"/> rows that don't exist
/// yet, matched by <see cref="ListingSource.Name"/>, and keeps the
/// compliance-relevant fields of ones that already exist in sync with
/// this list - not a one-shot "only if the table is empty" seed. That
/// matters twice over: a source added to this list later still shows up
/// in an already-seeded database, and a source whose vetting outcome
/// changes (e.g. BpostImmo/ProximusRealEstate going from "not vetted" to
/// "vetted and allowed" once a real collector existed for them) actually
/// takes effect there too, without needing a reset. Only the compliance
/// fields (<see cref="ListingSource.CollectionMethod"/>,
/// <see cref="ListingSource.Allowed"/>, <see cref="ListingSource.BaseUrl"/>,
/// <see cref="ListingSource.Notes"/>, the checked-at timestamps) are kept
/// in sync this way; <see cref="ListingSource.IsEnabled"/> and the run-
/// history fields are left alone once a row exists; they're runtime/user
/// state, not something code should silently overwrite. IsEnabled only
/// controls scheduling on top of Allowed anyway - it never overrides it
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
        var existing = await dbContext.ListingSources.ToDictionaryAsync(s => s.Name, cancellationToken);

        var checkedAt = new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc);
        var recheckedAt = new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

        var baseline = new List<ListingSource>
        {
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
                // Not scraped directly (sui generis database right, Livre
                // XI of the Code de droit economique - repeated/systematic
                // extraction is explicitly prohibited except via a
                // personal-use RSS feed that no longer exists on the live
                // site). Alert emails are parsed instead.
                Name = "2ememain",
                BaseUrl = "https://www.2ememain.be/l/immo/",
                IsEnabled = false,
                CollectionMethod = CollectionMethod.Email,
                Allowed = false,
                TermsCheckedAt = checkedAt,
                Notes = "ExternalAlertSource: terms of use invoke the sui generis database right against systematic/repeated extraction (Livre XI WER); the only carve-out is personal-use RSS (max 100 items), and no RSS feed for search results could be found on the live site. Website scraping stays Allowed=false permanently; alert-email parsing is the only sanctioned path (see TweedehandsEmailParser).",
                PollingIntervalMinutes = 15,
            },
            new ListingSource
            {
                // Umbrella source actually run by ListingCollectionBackgroundService:
                // EmailImportListingCollector fans out to every registered
                // IEmailListingParser (Immoweb/Immovlan/Zimmo/2ememain/agencies)
                // in one pass. Disabled until a real IEmailInbox replaces the
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
                IsEnabled = true,
                CollectionMethod = CollectionMethod.Html,
                Allowed = true,
                RobotsCheckedAt = recheckedAt,
                TermsCheckedAt = recheckedAt,
                Notes = "robots.txt permissive (only /backsite disallowed), no anti-bot protection, terms of use (checked at /fr/conditions-d-utilisation) have no scraping prohibition beyond a generic don't-harm-the-site clause. Listing pages embed a full JSON payload inline (Zabun platform, app.set(\"estateGroups\", ...)) - no separate detail-page fetches needed at all.",
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
                // Note: proximusrealestate.com ("ConnectImmo"), not
                // proximusforrealestate.be - that similarly-named domain is
                // a separate B2B fibre-for-developers marketing site with
                // no listings at all. The original URL seeded here was
                // wrong; corrected alongside vetting this collector.
                Name = "ProximusRealEstate",
                BaseUrl = "https://www.proximusrealestate.com/connectimmo/search.html",
                IsEnabled = true,
                CollectionMethod = CollectionMethod.Html,
                Allowed = true,
                RobotsCheckedAt = recheckedAt,
                TermsCheckedAt = recheckedAt,
                Notes = "robots.txt returns 404 (nothing disallowed), no anti-bot protection, no site-specific terms of use found prohibiting automated access (only Proximus's generic consumer-services legal page, not applicable to this B2B portal). Clean server-rendered HTML.",
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
                RobotsCheckedAt = recheckedAt,
                Notes = "Re-checked: robots.txt still permissive, no anti-bot protection, but listing pages mix an already-sold \"Behaalde prijs\" (price achieved) figure with unrelated \"similar listings\" carousel widgets repeating several other prices with no reliable field distinguishing the current listing's real asking price from a past sale or a neighboring card. That's a correctness risk (could show a sold property as available at a stale price), not just a parsing inconvenience - staying excluded until a reliable field is found, not just \"not done yet\".",
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
                RobotsCheckedAt = recheckedAt,
                Notes = "Re-checked: still an active Cloudflare challenge (cf-mitigated: challenge, HTTP 403) on belgiantrain.be. Allowed=false permanently unless that changes.",
                PollingIntervalMinutes = 60,
            },
        };

        foreach (var source in baseline)
        {
            if (existing.TryGetValue(source.Name, out var current))
            {
                current.BaseUrl = source.BaseUrl;
                current.CollectionMethod = source.CollectionMethod;
                current.Allowed = source.Allowed;
                current.Notes = source.Notes;
                current.RobotsCheckedAt = source.RobotsCheckedAt;
                current.TermsCheckedAt = source.TermsCheckedAt;
                // IsEnabled, PollingIntervalMinutes and the run-history
                // fields are deliberately left untouched - runtime/user
                // state, not code-owned.
            }
            else
            {
                dbContext.ListingSources.Add(source);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
