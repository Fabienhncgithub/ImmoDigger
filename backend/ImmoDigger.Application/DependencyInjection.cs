using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImmoDigger.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services. These only depend on Domain
    /// entities and Application-level repository interfaces, so they are
    /// registered independently from <c>Infrastructure.AddInfrastructure</c>
    /// (which provides the repository implementations).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IListingDeduplicationService, ListingDeduplicationService>();

        return services;
    }
}
