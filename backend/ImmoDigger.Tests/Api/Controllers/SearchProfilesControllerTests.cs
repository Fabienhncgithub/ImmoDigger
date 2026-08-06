using ImmoDigger.Api.Controllers;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Validators;
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

    private static SearchProfilesController CreateController(SearchProfileRepository repository) =>
        new(repository, new SearchProfileRequestValidator());

    [Fact]
    public async Task Create_PersistsProfile_AndReturnsCreatedResult()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new SearchProfileRepository(dbContext);
        var controller = CreateController(repository);

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
        var repository = new SearchProfileRepository(dbContext);
        var controller = CreateController(repository);

        var result = await controller.Create(CreateRequest(name: ""), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new SearchProfileRepository(dbContext);
        var controller = CreateController(repository);
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
        var repository = new SearchProfileRepository(dbContext);
        var controller = CreateController(repository);
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
        var repository = new SearchProfileRepository(dbContext);
        var controller = CreateController(repository);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_RemovesProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new SearchProfileRepository(dbContext);
        var controller = CreateController(repository);
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
        var repository = new SearchProfileRepository(dbContext);
        var controller = CreateController(repository);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
