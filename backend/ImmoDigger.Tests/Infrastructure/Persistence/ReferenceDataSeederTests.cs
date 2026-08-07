using ImmoDigger.Infrastructure.Persistence.DemoData;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Tests.Infrastructure.Persistence;

public class ReferenceDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesTheFiveV1Sources()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var names = await dbContext.ListingSources.Select(s => s.Name).ToListAsync();

        Assert.Contains("Biddit", names);
        Assert.Contains("Immoweb", names);
        Assert.Contains("Immovlan", names);
        Assert.Contains("Zimmo", names);
        Assert.Contains("GenericAgency", names);
    }

    [Fact]
    public async Task SeedAsync_CreatesTheInstitutionalSources()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var names = await dbContext.ListingSources.Select(s => s.Name).ToListAsync();

        Assert.Contains("BpostImmo", names);
        Assert.Contains("RegieDesBatiments", names);
        Assert.Contains("ProximusRealEstate", names);
        Assert.Contains("VlaamseOverheidVastgoed", names);
        Assert.Contains("SncbImmo", names);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_WhenSourcesAlreadyExist()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);
        var countAfterFirstSeed = await dbContext.ListingSources.CountAsync();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        Assert.Equal(countAfterFirstSeed, await dbContext.ListingSources.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_DisablesTheGenericAgencyPlaceholderByDefault()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var genericAgency = await dbContext.ListingSources.SingleAsync(s => s.Name == "GenericAgency");

        Assert.False(genericAgency.IsEnabled);
    }

    [Fact]
    public async Task SeedAsync_DisablesInstitutionalSourcesWithoutAVettedCollector()
    {
        // RegieDesBatiments now has a real, vetted collector (clean
        // server-rendered HTML, no anti-bot protection - see its class doc
        // comment) so it is the one institutional source seeded enabled.
        // The rest still have no collector implemented and must never come
        // up enabled.
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var institutional = await dbContext.ListingSources
            .Where(s => new[] { "BpostImmo", "RegieDesBatiments", "ProximusRealEstate", "VlaamseOverheidVastgoed", "SncbImmo" }.Contains(s.Name))
            .ToListAsync();

        Assert.Equal(5, institutional.Count);
        Assert.All(institutional.Where(s => s.Name != "RegieDesBatiments"), s => Assert.False(s.IsEnabled));

        var regieDesBatiments = institutional.Single(s => s.Name == "RegieDesBatiments");
        Assert.True(regieDesBatiments.IsEnabled);
        Assert.True(regieDesBatiments.Allowed);
    }

    [Fact]
    public async Task SeedAsync_MarksImmowebImmovlanZimmo_AsEmailOnlyAndNotAllowedForDirectScraping()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var externalAlertSources = await dbContext.ListingSources
            .Where(s => new[] { "Immoweb", "Immovlan", "Zimmo", "2ememain" }.Contains(s.Name))
            .ToListAsync();

        Assert.Equal(4, externalAlertSources.Count);
        Assert.All(externalAlertSources, s =>
        {
            Assert.Equal(ImmoDigger.Domain.Common.CollectionMethod.Email, s.CollectionMethod);
            Assert.False(s.Allowed);
            Assert.False(s.IsEnabled);
        });
    }
}
