using ImmoDigger.Api.Controllers;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Tests.Api.Controllers;

public class SourcesControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsSourcesWithListingCounts()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var sourceRepository = new ListingSourceRepository(dbContext);
        var listingRepository = new PropertyListingRepository(dbContext);
        await sourceRepository.AddAsync(new ListingSource { Name = "Immoweb", BaseUrl = "https://immoweb.be" });
        await listingRepository.AddAsync(new PropertyListing
        {
            Source = "Immoweb", ExternalId = "1", Url = "https://immoweb.be/1", Title = "T",
            SaleType = "RegularSale", PropertyType = "IncomeBuilding", RawContentHash = "h",
            FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
        });
        await sourceRepository.SaveChangesAsync();
        var controller = new SourcesController(sourceRepository, listingRepository);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IReadOnlyList<SourceDto>>(ok.Value);
        Assert.Equal(1, dtos.Single(d => d.Name == "Immoweb").ListingCount);
    }

    [Fact]
    public async Task Update_TogglesActivation()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var sourceRepository = new ListingSourceRepository(dbContext);
        var listingRepository = new PropertyListingRepository(dbContext);
        var source = new ListingSource { Name = "Immoweb", BaseUrl = "https://immoweb.be", IsEnabled = true };
        await sourceRepository.AddAsync(source);
        await sourceRepository.SaveChangesAsync();
        var controller = new SourcesController(sourceRepository, listingRepository);

        var result = await controller.Update(
            source.Id, new UpdateSourceRequest(IsEnabled: false, PollingIntervalMinutes: 30), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SourceDto>(ok.Value);
        Assert.False(dto.IsEnabled);
        Assert.Equal(30, dto.PollingIntervalMinutes);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = new SourcesController(
            new ListingSourceRepository(dbContext), new PropertyListingRepository(dbContext));

        var result = await controller.Update(
            Guid.NewGuid(), new UpdateSourceRequest(true, null), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
