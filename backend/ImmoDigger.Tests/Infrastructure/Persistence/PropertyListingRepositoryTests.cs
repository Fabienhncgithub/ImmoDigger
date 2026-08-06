using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.Persistence.Repositories;

namespace ImmoDigger.Tests.Infrastructure.Persistence;

public class PropertyListingRepositoryTests
{
    private static PropertyListing CreateListing(string externalId = "EXT-1") => new()
    {
        Source = "Immoweb",
        ExternalId = externalId,
        Url = "https://example.invalid/listing/1",
        Title = "Immeuble de rapport",
        SaleType = "RegularSale",
        PropertyType = "IncomeBuilding",
        RawContentHash = "hash-1",
        FirstSeenAt = DateTime.UtcNow,
        LastSeenAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsTheListing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();

        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(listing.Id);
        Assert.NotNull(stored);
        Assert.Equal(listing.Title, stored!.Title);
    }

    [Fact]
    public async Task GetBySourceAndExternalIdAsync_FindsAMatchingListing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing("EXT-42");
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();

        var found = await repository.GetBySourceAndExternalIdAsync("Immoweb", "EXT-42");

        Assert.NotNull(found);
        Assert.Equal(listing.Id, found!.Id);
    }

    [Fact]
    public async Task GetBySourceAndExternalIdAsync_ReturnsNull_WhenNoMatch()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);

        var found = await repository.GetBySourceAndExternalIdAsync("Immoweb", "does-not-exist");

        Assert.Null(found);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var listing = CreateListing();
        await repository.AddAsync(listing);
        await repository.SaveChangesAsync();

        listing.AskingPrice = 350_000m;
        repository.Update(listing);
        await repository.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(listing.Id);
        Assert.Equal(350_000m, stored!.AskingPrice);
    }
}
