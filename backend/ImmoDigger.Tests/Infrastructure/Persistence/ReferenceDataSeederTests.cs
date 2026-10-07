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
    public async Task SeedAsync_AddsAMissingBaselineSource_WithoutDuplicatingExistingOnes()
    {
        // Simulates a database seeded before a source (here "2ememain")
        // was added to the baseline list - the seeder must fill the gap
        // on the next run rather than only ever seeding an empty table.
        await using var dbContext = TestDbContextFactory.Create();
        await ReferenceDataSeeder.SeedAsync(dbContext);
        var twoememain = await dbContext.ListingSources.SingleAsync(s => s.Name == "2ememain");
        dbContext.ListingSources.Remove(twoememain);
        await dbContext.SaveChangesAsync();
        var countAfterRemoval = await dbContext.ListingSources.CountAsync();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        Assert.Equal(countAfterRemoval + 1, await dbContext.ListingSources.CountAsync());
        Assert.Equal(1, await dbContext.ListingSources.CountAsync(s => s.Name == "2ememain"));
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
    public async Task SeedAsync_EnablesInstitutionalSourcesWithAVettedCollector_KeepsTheRestDisabled()
    {
        // RegieDesBatiments, BpostImmo and ProximusRealEstate now have real,
        // vetted collectors (see each collector's class doc comment) so
        // they're seeded enabled; VlaamseOverheidVastgoed (unreliable price
        // field) and SncbImmo (Cloudflare) still have none and must never
        // come up enabled.
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var institutional = await dbContext.ListingSources
            .Where(s => new[] { "BpostImmo", "RegieDesBatiments", "ProximusRealEstate", "VlaamseOverheidVastgoed", "SncbImmo" }.Contains(s.Name))
            .ToListAsync();

        Assert.Equal(5, institutional.Count);

        var vetted = new[] { "BpostImmo", "RegieDesBatiments", "ProximusRealEstate" };
        Assert.All(institutional.Where(s => vetted.Contains(s.Name)), s =>
        {
            Assert.True(s.IsEnabled, $"{s.Name} should be enabled");
            Assert.True(s.Allowed, $"{s.Name} should be allowed");
        });
        Assert.All(institutional.Where(s => !vetted.Contains(s.Name)), s =>
        {
            Assert.False(s.IsEnabled, $"{s.Name} should stay disabled");
            Assert.False(s.Allowed, $"{s.Name} should stay not-allowed");
        });
    }

    [Fact]
    public async Task SeedAsync_UpdatesComplianceFields_ForAnAlreadySeededSource_WithoutTouchingIsEnabled()
    {
        // Simulates a source whose vetting outcome changed after a user's
        // database was already seeded (e.g. BpostImmo going from
        // "not vetted" to "vetted and allowed" once a collector existed):
        // re-seeding must pick up the new Allowed/CollectionMethod/Notes,
        // but must never silently flip a user's own IsEnabled choice.
        await using var dbContext = TestDbContextFactory.Create();
        await ReferenceDataSeeder.SeedAsync(dbContext);

        var bpostImmo = await dbContext.ListingSources.SingleAsync(s => s.Name == "BpostImmo");
        bpostImmo.Allowed = false;
        bpostImmo.CollectionMethod = ImmoDigger.Domain.Common.CollectionMethod.Disabled;
        bpostImmo.IsEnabled = false; // simulates the user having disabled it manually
        await dbContext.SaveChangesAsync();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var reloaded = await dbContext.ListingSources.SingleAsync(s => s.Name == "BpostImmo");
        Assert.True(reloaded.Allowed);
        Assert.Equal(ImmoDigger.Domain.Common.CollectionMethod.Html, reloaded.CollectionMethod);
        Assert.False(reloaded.IsEnabled); // untouched, even though the baseline says true
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

    [Fact]
    public async Task SeedAsync_AddsTheExpandedOfficialAlertSources()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var sources = await dbContext.ListingSources
            .Where(source => new[] { "Spotto", "Immoscoop", "Realo" }.Contains(source.Name))
            .ToListAsync();
        Assert.Equal(3, sources.Count);
        Assert.All(sources, source =>
        {
            Assert.Equal(ImmoDigger.Domain.Common.CollectionMethod.Email, source.CollectionMethod);
            Assert.False(source.Allowed);
            Assert.False(source.IsEnabled);
        });
    }
}
