using ImmoDigger.Api.Controllers;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Tests.Api.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetSummary_ComputesAggregatesAndRecentListings()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);

        await repository.AddAsync(new PropertyListing
        {
            Source = "Immoweb", ExternalId = "1", Url = "https://x.invalid/1", Title = "Recent, high risk",
            SaleType = "RegularSale", PropertyType = "IncomeBuilding", RawContentHash = "h1",
            AskingPrice = 300_000m, OpportunityScore = 80m, RiskLevel = "High", IsActive = true,
            FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
        });
        await repository.AddAsync(new PropertyListing
        {
            Source = "Immoweb", ExternalId = "2", Url = "https://x.invalid/2", Title = "Older, inactive",
            SaleType = "RegularSale", PropertyType = "IncomeBuilding", RawContentHash = "h2",
            AskingPrice = 500_000m, OpportunityScore = 40m, RiskLevel = "Low", IsActive = false,
            FirstSeenAt = DateTime.UtcNow.AddDays(-30), LastSeenAt = DateTime.UtcNow.AddDays(-30),
        });
        await repository.AddAsync(new PropertyListing
        {
            Source = "Demo", ExternalId = "DEMO-1", Url = "https://x.invalid/demo", Title = "Demo listing",
            SaleType = "RegularSale", PropertyType = "IncomeBuilding", RawContentHash = "demo",
            AskingPrice = 900_000m, OpportunityScore = 100m, RiskLevel = "High", IsActive = true,
            FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
        });
        await repository.SaveChangesAsync();
        var controller = new DashboardController(repository);

        var result = await controller.GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var summary = Assert.IsType<DashboardSummaryDto>(ok.Value);
        Assert.Equal(1, summary.NewListingsToday);
        Assert.Equal(2, summary.ActiveListingsCount);
        Assert.Equal(1, summary.RealActiveListingsCount);
        Assert.Equal(1, summary.DemoActiveListingsCount);
        Assert.Equal(1, summary.PricedActiveListingsCount);
        Assert.Equal(1, summary.ScoredActiveListingsCount);
        Assert.Equal(300_000m, summary.AveragePrice);
        Assert.Equal(1, summary.StrongOpportunitiesCount);
        Assert.Equal(1, summary.HighRiskCount);
        Assert.Equal(2, summary.RecentListings.Count);
    }
}
