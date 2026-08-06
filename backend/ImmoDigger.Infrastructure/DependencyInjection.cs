using ImmoDigger.Application.Interfaces;
using ImmoDigger.Infrastructure.BackgroundServices;
using ImmoDigger.Infrastructure.Collectors;
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

        services.Configure<CollectionOptions>(configuration.GetSection(CollectionOptions.SectionName));

        // Real, ToS-compliant collectors for Biddit/Immoweb/Immovlan/Zimmo
        // are added once each source has been vetted (public API/RSS
        // availability, terms of use). Until then, GenericAgencyPlaceholderCollector
        // exercises the collection framework end to end; its matching
        // "GenericAgency" source is seeded disabled.
        services.AddScoped<IListingCollector, GenericAgencyPlaceholderCollector>();

        // Registered as itself (singleton) in addition to being hosted, so
        // "POST /api/collection/run" can resolve the same instance and
        // trigger a cycle on demand rather than only on the timer's tick.
        services.AddSingleton<ListingCollectionBackgroundService>();
        services.AddHostedService(sp => sp.GetRequiredService<ListingCollectionBackgroundService>());

        return services;
    }
}
