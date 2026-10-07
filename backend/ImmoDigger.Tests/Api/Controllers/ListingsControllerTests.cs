using ImmoDigger.Api.Controllers;
using ImmoDigger.Application.Common;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Services;
using ImmoDigger.Application.Validators;
using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Tests.Api.Controllers;

public class ListingsControllerTests
{
    private static PropertyListing CreateListing(
        string city = "Uccle", string postalCode = "1180", string propertyType = "IncomeBuilding") => new()
    {
        Source = "Immoweb",
        ExternalId = Guid.NewGuid().ToString(),
        Url = $"https://example.invalid/listing/{Guid.NewGuid()}",
        Title = "Immeuble de rapport",
        City = city,
        PostalCode = postalCode,
        SaleType = "RegularSale",
        PropertyType = propertyType,
        RawContentHash = Guid.NewGuid().ToString(),
        AskingPrice = 400_000m,
        FirstSeenAt = DateTime.UtcNow,
        LastSeenAt = DateTime.UtcNow,
    };

    private static ListingsController CreateController(PropertyListingRepository repository) =>
        new(repository, new InvestmentAnalysisService(), new UpdateListingRequestValidator());

    [Fact]
    public async Task GetListings_FiltersByCity()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        await repository.AddAsync(CreateListing(city: "Uccle"));
        await repository.AddAsync(CreateListing(city: "Forest"));
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.GetListings(
            new ListingQueryParameters { Cities = ["Uccle"] }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<ListingSummaryDto>>(ok.Value);
        Assert.Single(paged.Items);
        Assert.Equal("Uccle", paged.Items[0].City);
    }

    [Fact]
    public async Task GetListings_FiltersByMultipleCities()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        await repository.AddAsync(CreateListing(city: "Uccle"));
        await repository.AddAsync(CreateListing(city: "Forest"));
        await repository.AddAsync(CreateListing(city: "Anderlecht"));
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.GetListings(
            new ListingQueryParameters { Cities = ["Uccle", "Forest"] }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<ListingSummaryDto>>(ok.Value);
        Assert.Equal(2, paged.Items.Count);
        Assert.All(paged.Items, item => Assert.Contains(item.City, new[] { "Uccle", "Forest" }));
    }

    [Fact]
    public async Task GetListings_FiltersByPropertyType()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        await repository.AddAsync(CreateListing(propertyType: "IncomeBuilding"));
        await repository.AddAsync(CreateListing(propertyType: "ApartmentBuilding"));
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.GetListings(
            new ListingQueryParameters { PropertyTypes = ["IncomeBuilding"] }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<ListingSummaryDto>>(ok.Value);
        Assert.Single(paged.Items);
        Assert.Equal("IncomeBuilding", paged.Items[0].PropertyType);
    }

    [Fact]
    public async Task GetListing_ReturnsNotFound_ForUnknownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var controller = CreateController(repository);

        var result = await controller.GetListing(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetListing_ReturnsDetail_ForKnownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.GetListing(listing.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ListingDetailDto>(ok.Value);
        Assert.Equal(listing.Id, dto.Id);
    }

    [Fact]
    public async Task Analyze_ComputesAndPersistsScoreAndRisk()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        listing.PebRating = "G";
        listing.LivingArea = 200m;
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.Analyze(listing.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AnalyzeListingResponse>(ok.Value);
        Assert.NotNull(response.Listing.OpportunityScore);
        Assert.Equal("Medium", response.RiskAssessment.RiskLevel);

        var reloaded = await repository.GetByIdAsync(listing.Id);
        Assert.NotNull(reloaded!.OpportunityScore);
        Assert.Equal("Medium", reloaded.RiskLevel);
    }

    [Fact]
    public async Task UpdateListing_AppliesEditableFields()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var request = new UpdateListingRequest("Belle opportunite", 750m, 40_000m, 60_000m);
        var result = await controller.UpdateListing(listing.Id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ListingDetailDto>(ok.Value);
        Assert.Equal("Belle opportunite", dto.PersonalNotes);
        Assert.Equal(750m, dto.EstimatedMonthlyRentPerUnit);
    }

    [Fact]
    public async Task UpdateListing_ReturnsValidationProblem_ForNegativeRent()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var request = new UpdateListingRequest(null, -100m, null, null);
        var result = await controller.UpdateListing(listing.Id, request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task MarkReviewed_SetsReviewFlagAndTimestamp()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.MarkReviewed(listing.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ListingDetailDto>(ok.Value);
        Assert.True(dto.IsReviewed);
        Assert.NotNull(dto.ReviewedAt);
    }

    [Fact]
    public async Task GetPriceHistory_ReturnsEntriesOrderedByDate()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        listing.PriceHistory.Add(new ListingPriceHistory { Price = 420_000m, RecordedAt = DateTime.UtcNow.AddDays(-10) });
        listing.PriceHistory.Add(new ListingPriceHistory { Price = 400_000m, RecordedAt = DateTime.UtcNow.AddDays(-2) });
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.GetPriceHistory(listing.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsAssignableFrom<IReadOnlyList<PriceHistoryEntryDto>>(ok.Value);
        Assert.Equal(2, history.Count);
        Assert.Equal(420_000m, history[0].Price);
        Assert.Equal(400_000m, history[1].Price);
    }

    [Fact]
    public async Task GetPriceHistory_ReturnsNotFound_ForUnknownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var controller = CreateController(repository);

        var result = await controller.GetPriceHistory(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteListing_RemovesTheListing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();
        var controller = CreateController(repository);

        var result = await controller.DeleteListing(listing.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await repository.GetByIdAsync(listing.Id));
    }

    [Fact]
    public async Task DeleteListing_ReturnsNotFound_ForUnknownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var controller = CreateController(repository);

        var result = await controller.DeleteListing(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
