using ImmoDigger.Domain.Common;
using ImmoDigger.Infrastructure.Persistence.DemoData;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Tests.Infrastructure.Persistence;

public class DemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesTenFictionalListings()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await DemoDataSeeder.SeedAsync(dbContext);

        Assert.Equal(10, await dbContext.PropertyListings.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_IncludesTheAvenueCoghenRiskScenario()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await DemoDataSeeder.SeedAsync(dbContext);

        var coghen = await dbContext.PropertyListings.SingleAsync(l => l.ExternalId == "DEMO-BIDDIT-001");

        Assert.Equal("Uccle", coghen.City);
        Assert.Equal("1180", coghen.PostalCode);
        Assert.Equal(400_000m, coghen.AskingPrice);
        Assert.Equal(4, coghen.ObservedUnitCount);
        Assert.Equal(3, coghen.OfficialUnitCount);
        Assert.Equal("G", coghen.PebRating);
        Assert.False(coghen.ElectricalInstallationCompliant);
        Assert.Equal(RiskLevel.High, coghen.RiskLevel);
    }

    [Fact]
    public async Task SeedAsync_IncludesAPriceDropScenario()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await DemoDataSeeder.SeedAsync(dbContext);

        var listing = await dbContext.PropertyListings
            .Include(l => l.PriceHistory)
            .SingleAsync(l => l.ExternalId == "DEMO-IMMOWEB-007");

        var orderedPrices = listing.PriceHistory
            .OrderBy(h => h.RecordedAt)
            .Select(h => h.Price)
            .ToList();

        Assert.Equal(2, orderedPrices.Count);
        Assert.True(orderedPrices[1] < orderedPrices[0]);
    }

    [Fact]
    public async Task SeedAsync_IncludesAnUrbanisticRiskScenario()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await DemoDataSeeder.SeedAsync(dbContext);

        var listing = await dbContext.PropertyListings.SingleAsync(l => l.ExternalId == "DEMO-BIDDIT-006");

        Assert.Equal(RiskLevel.High, listing.RiskLevel);
        Assert.Contains("urbanistique", listing.RiskSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SeedAsync_CreatesTheDemoSearchProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await DemoDataSeeder.SeedAsync(dbContext);

        var profile = await dbContext.SearchProfiles.SingleAsync();

        Assert.Equal("Immeuble Bruxelles", profile.Name);
        Assert.Equal(750_000m, profile.MaximumPrice);
        Assert.Equal(65m, profile.MinimumOpportunityScore);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_WhenListingsAlreadyExist()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await DemoDataSeeder.SeedAsync(dbContext);
        await DemoDataSeeder.SeedAsync(dbContext);

        Assert.Equal(10, await dbContext.PropertyListings.CountAsync());
    }
}
