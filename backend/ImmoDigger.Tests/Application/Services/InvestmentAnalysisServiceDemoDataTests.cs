using ImmoDigger.Application.Services;
using ImmoDigger.Infrastructure.Persistence.DemoData;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Tests.Application.Services;

/// <summary>
/// Sanity check: the analysis service must handle every demo listing
/// (including the Ganshoren fixture with a missing asking price) without
/// throwing, and always produce a score within [0, 100].
/// </summary>
public class InvestmentAnalysisServiceDemoDataTests
{
    [Fact]
    public async Task Analyze_HandlesAllDemoListings_WithoutThrowing_AndProducesAScoreWithinRange()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await DemoDataSeeder.SeedAsync(dbContext);
        var listings = await dbContext.PropertyListings.ToListAsync();

        var sut = new InvestmentAnalysisService();

        foreach (var listing in listings)
        {
            sut.Analyze(listing);

            Assert.NotNull(listing.OpportunityScore);
            Assert.InRange(listing.OpportunityScore!.Value, 0m, 100m);
            Assert.False(string.IsNullOrWhiteSpace(listing.RiskLevel));
            Assert.False(string.IsNullOrWhiteSpace(listing.RiskSummary));
        }
    }
}
