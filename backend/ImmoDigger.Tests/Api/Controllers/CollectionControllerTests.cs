using ImmoDigger.Api.Controllers;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.BackgroundServices;
using ImmoDigger.Infrastructure.Persistence;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ImmoDigger.Tests.Api.Controllers;

public class CollectionControllerTests
{
    private static (CollectionController Controller, ServiceProvider Provider) CreateController()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<ImmoDiggerDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddScoped<IListingSourceRepository, ListingSourceRepository>();
        services.AddScoped<IPropertyListingRepository, PropertyListingRepository>();
        services.AddSingleton<IOptions<CollectionOptions>>(Options.Create(new CollectionOptions()));

        var provider = services.BuildServiceProvider();
        var backgroundService = new ListingCollectionBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<CollectionOptions>>(),
            NullLogger<ListingCollectionBackgroundService>.Instance);

        var controller = new CollectionController(
            provider.GetRequiredService<IListingSourceRepository>(),
            provider.GetRequiredService<IPropertyListingRepository>(),
            backgroundService);

        return (controller, provider);
    }

    [Fact]
    public void Run_ReturnsAccepted()
    {
        var (controller, provider) = CreateController();
        using var _ = provider;

        var result = controller.Run();

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task Status_ReturnsSourcesAndLastRunAt()
    {
        var (controller, provider) = CreateController();
        using var _ = provider;

        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ImmoDiggerDbContext>();
            dbContext.ListingSources.Add(new ListingSource
            {
                Name = "Immoweb",
                BaseUrl = "https://immoweb.be",
                LastSuccessfulRunAt = DateTime.UtcNow.AddMinutes(-5),
            });
            await dbContext.SaveChangesAsync();
        }

        var result = await controller.Status(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<CollectionStatusDto>(ok.Value);
        Assert.Single(status.Sources);
        Assert.NotNull(status.LastRunAt);
    }
}
