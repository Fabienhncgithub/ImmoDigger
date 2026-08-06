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

        Assert.Equal(5, names.Count);
        Assert.Contains("Biddit", names);
        Assert.Contains("Immoweb", names);
        Assert.Contains("Immovlan", names);
        Assert.Contains("Zimmo", names);
        Assert.Contains("GenericAgency", names);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_WhenSourcesAlreadyExist()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);
        await ReferenceDataSeeder.SeedAsync(dbContext);

        Assert.Equal(5, await dbContext.ListingSources.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_DisablesTheGenericAgencyPlaceholderByDefault()
    {
        await using var dbContext = TestDbContextFactory.Create();

        await ReferenceDataSeeder.SeedAsync(dbContext);

        var genericAgency = await dbContext.ListingSources.SingleAsync(s => s.Name == "GenericAgency");

        Assert.False(genericAgency.IsEnabled);
    }
}
