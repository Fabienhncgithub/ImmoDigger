using ImmoDigger.Api.Controllers;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Validators;
using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.Persistence;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Tests.Api.Controllers;

public class SearchProfilesControllerTests
{
    private static SearchProfileRequest CreateRequest(string name = "Immeuble Bruxelles") => new(
        Name: name,
        MaximumPrice: 750_000m,
        MinimumGrossYield: 5.5m,
        MinimumUnitCount: 3,
        MinimumLivingArea: null,
        RequireGarage: false,
        IncludePublicSales: true,
        PostalCodes: ["1180", "1190", "1060"],
        PropertyTypes: ["IncomeBuilding"],
        MinimumOpportunityScore: 65m,
        IsEnabled: true);

    private static SearchProfilesController CreateController(ImmoDiggerDbContext dbContext) =>
        new(
            new SearchProfileRepository(dbContext),
            new PropertyListingRepository(dbContext),
            new SearchProfileRequestValidator());

    private static PropertyListing CreateActiveListing(
        string postalCode = "1180",
        string propertyType = "IncomeBuilding",
        decimal price = 700_000m,
        int units = 4,
        decimal score = 70m,
        decimal grossYield = 6m) =>
        new()
        {
            Source = "Test",
            ExternalId = Guid.NewGuid().ToString(),
            Url = "https://example.invalid/" + Guid.NewGuid(),
            Title = "Test listing",
            SaleType = "RegularSale",
            PropertyType = propertyType,
            RawContentHash = Guid.NewGuid().ToString(),
            PostalCode = postalCode,
            AskingPrice = price,
            OfficialUnitCount = units,
            OpportunityScore = score,
            EstimatedGrossYield = grossYield,
            IsActive = true,
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task Create_PersistsProfile_AndReturnsCreatedResult()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(dbContext);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<SearchProfileDto>(created.Value);
        Assert.Equal("Immeuble Bruxelles", dto.Name);
        Assert.Equal(1, await dbContext.SearchProfiles.CountAsync());
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNameIsEmpty()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(dbContext);

        var result = await controller.Create(CreateRequest(name: ""), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsTheNumberOfActiveListingsAlreadyMatchingTheProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.PropertyListings.AddRange(
            CreateActiveListing(), // matches every criterion in CreateRequest()
            CreateActiveListing(postalCode: "9000")); // wrong postal code - does not match
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<SearchProfileDto>(created.Value);
        Assert.Equal(1, dto.MatchingListingsCount);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(dbContext);
        await controller.Create(CreateRequest("A"), CancellationToken.None);
        await controller.Create(CreateRequest("B"), CancellationToken.None);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profiles = Assert.IsAssignableFrom<IReadOnlyList<SearchProfileDto>>(ok.Value);
        Assert.Equal(2, profiles.Count);
    }

    [Fact]
    public async Task Update_ReplacesFields()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(dbContext);
        var createResult = await controller.Create(CreateRequest(), CancellationToken.None);
        var created = (SearchProfileDto)((CreatedAtActionResult)createResult.Result!).Value!;

        var updateRequest = CreateRequest("Immeuble Bruxelles (mis a jour)") with { MaximumPrice = 900_000m };
        var result = await controller.Update(created.Id, updateRequest, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SearchProfileDto>(ok.Value);
        Assert.Equal("Immeuble Bruxelles (mis a jour)", dto.Name);
        Assert.Equal(900_000m, dto.MaximumPrice);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(dbContext);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_RemovesProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(dbContext);
        var createResult = await controller.Create(CreateRequest(), CancellationToken.None);
        var created = (SearchProfileDto)((CreatedAtActionResult)createResult.Result!).Value!;

        var result = await controller.Delete(created.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await dbContext.SearchProfiles.CountAsync());
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_ForUnknownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(dbContext);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
