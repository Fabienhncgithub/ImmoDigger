using ImmoDigger.Application.Interfaces;
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

        return services;
    }
}
