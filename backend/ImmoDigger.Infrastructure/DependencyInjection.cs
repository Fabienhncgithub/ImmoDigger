using ImmoDigger.Application.Interfaces;
using ImmoDigger.Infrastructure.BackgroundServices;
using ImmoDigger.Infrastructure.Collectors;
using ImmoDigger.Infrastructure.Collectors.Biddit;
using ImmoDigger.Infrastructure.Collectors.BpostImmo;
using ImmoDigger.Infrastructure.Collectors.ProximusRealEstate;
using ImmoDigger.Infrastructure.Collectors.RegieDesBatiments;
using ImmoDigger.Infrastructure.EmailImport;
using ImmoDigger.Infrastructure.EmailImport.Parsers;
using ImmoDigger.Infrastructure.Import;
using ImmoDigger.Infrastructure.Persistence;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImmoDigger.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the PostgreSQL <see cref="ImmoDiggerDbContext"/> and the
    /// repositories built on top of it. The connection string is read from
    /// configuration key "ConnectionStrings:Postgres" (settable via
    /// ASP.NET Core's standard ConnectionStrings__Postgres environment
    /// variable or user-secrets), falling back to the POSTGRES_CONNECTION_STRING
    /// environment variable named in the project's .env.example. Never
    /// committed to source control.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing PostgreSQL connection string. Set ConnectionStrings:Postgres " +
                "(user-secrets/ConnectionStrings__Postgres) or the POSTGRES_CONNECTION_STRING " +
                "environment variable.");
        }

        services.AddDbContext<ImmoDiggerDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IPropertyListingRepository, PropertyListingRepository>();
        services.AddScoped<IListingSourceRepository, ListingSourceRepository>();
        services.AddScoped<ISearchProfileRepository, SearchProfileRepository>();
        services.AddScoped<IProcessedEmailMessageRepository, ProcessedEmailMessageRepository>();

        services.Configure<CollectionOptions>(configuration.GetSection(CollectionOptions.SectionName));

        // Immoweb (explicit anti-scraping terms with penalty clauses),
        // Immovlan (blocks automated requests at the network/WAF level)
        // and Zimmo/SNCB-belgiantrain (Cloudflare bot-fingerprinting) were
        // checked and ruled out: no collector exists for them, and none
        // should be added without a genuine change in what those sites
        // allow. GenericAgencyPlaceholderCollector keeps exercising the
        // framework end to end; its "GenericAgency" source is seeded
        // disabled.
        services.AddScoped<IListingCollector, GenericAgencyPlaceholderCollector>();

        // Biddit and the Regie des Batiments were vetted (robots.txt,
        // terms of use where reachable, absence of anti-bot protection)
        // before being wired up - see each collector's class doc comment
        // for exactly what was checked and how.
        services.AddHttpClient(BidditCollector.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://www.biddit.be");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CollectorConstants.UserAgent);
        });
        services.AddScoped<IListingCollector, BidditCollector>();

        services.AddHttpClient(RegieDesBatimentsCollector.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://www.regiedesbatiments.be");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CollectorConstants.UserAgent);
        });
        services.AddScoped<IListingCollector, RegieDesBatimentsCollector>();

        // BpostImmo and ProximusRealEstate were vetted the same way (see
        // each collector's class doc comment): permissive robots.txt, no
        // anti-bot protection, no scraping prohibition in reachable terms
        // of use. VlaamseOverheidVastgoed and SncbImmo were checked again
        // and stay excluded - the former mixes already-sold "behaalde
        // prijs" figures into the same widgets as active listings with no
        // reliable field to tell them apart (a correctness risk, not just
        // a parsing inconvenience), the latter still sits behind an active
        // Cloudflare challenge.
        services.AddHttpClient(BpostImmoCollector.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://bpostimmo.be");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CollectorConstants.UserAgent);
        });
        services.AddScoped<IListingCollector, BpostImmoCollector>();

        services.AddHttpClient(ProximusRealEstateCollector.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://www.proximusrealestate.com");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CollectorConstants.UserAgent);
        });
        services.AddScoped<IListingCollector, ProximusRealEstateCollector>();

        // Email-import pipeline (major Belgian portals + agencies):
        // never touches those sites directly, only parses alert emails
        // already sitting in the user's own inbox. Falls back to the
        // no-op NullEmailInbox when no mailbox is configured (Imap:Host
        // unset) - the app runs fine either way, it just imports nothing
        // by this route until real credentials are set (user-secrets/
        // Imap__Host etc., same pattern as ConnectionStrings:Postgres -
        // see ImapSettings's doc comment). Agency-specific parsers are
        // added by registering more AgencyEmailParser instances, not new
        // files.
        services.Configure<ImapSettings>(configuration.GetSection(ImapSettings.SectionName));
        var imapHost = configuration[$"{ImapSettings.SectionName}:Host"];
        if (!string.IsNullOrWhiteSpace(imapHost))
        {
            services.AddScoped<IEmailInbox, ImapEmailInbox>();
        }
        else
        {
            services.AddScoped<IEmailInbox, NullEmailInbox>();
        }

        services.AddScoped<IEmailListingParser, ImmowebEmailParser>();
        services.AddScoped<IEmailListingParser, ImmovlanEmailParser>();
        services.AddScoped<IEmailListingParser, ZimmoEmailParser>();
        services.AddScoped<IEmailListingParser, TweedehandsEmailParser>();
        services.AddScoped<IEmailListingParser, SpottoEmailParser>();
        services.AddScoped<IEmailListingParser, ImmoscoopEmailParser>();
        services.AddScoped<IEmailListingParser, RealoEmailParser>();
        services.AddScoped<IListingCollector, EmailImportListingCollector>();

        // Manual single-URL import ("POST /api/import/url"): a one-off,
        // user-initiated fetch of a page's own Open Graph metadata, not a
        // crawl - see OpenGraphManualImportService's doc comment.
        services.AddHttpClient(OpenGraphManualImportService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CollectorConstants.UserAgent);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // Redirects are deliberately not followed: a public URL could
            // otherwise redirect the server to a private/internal address.
            AllowAutoRedirect = false,
        });
        services.AddSingleton<IManualImportUrlSafetyChecker, ManualImportUrlSafetyChecker>();
        services.AddScoped<IManualListingImportService, OpenGraphManualImportService>();

        // Registered as itself (singleton) in addition to being hosted, so
        // "POST /api/collection/run" can resolve the same instance and
        // trigger a cycle on demand rather than only on the timer's tick.
        services.AddSingleton<ListingCollectionBackgroundService>();
        services.AddHostedService(sp => sp.GetRequiredService<ListingCollectionBackgroundService>());

        return services;
    }
}
