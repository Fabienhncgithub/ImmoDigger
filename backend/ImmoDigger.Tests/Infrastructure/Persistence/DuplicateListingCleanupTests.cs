using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Tests.Infrastructure.Persistence;

public class DuplicateListingCleanupTests
{
    private static PropertyListing CreateListing(
        string source, string externalId, DateTime firstSeenAt,
        string postalCode = "1000", decimal? price = 1_440_000m, decimal? area = 901m) => new()
    {
        Source = source,
        ExternalId = externalId,
        Url = $"https://example.invalid/{Guid.NewGuid()}",
        Title = "Immeuble de rapport",
        SaleType = "RegularSale",
        PropertyType = "IncomeBuilding",
        RawContentHash = Guid.NewGuid().ToString(),
        PostalCode = postalCode,
        AskingPrice = price,
        LivingArea = area,
        FirstSeenAt = firstSeenAt,
        LastSeenAt = firstSeenAt,
    };

    private static readonly DateTime Day1 = new(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RunAsync_KeepsOneRowPerSourceAndExternalId_PreferringTheOneTheUserWorkedOn()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var oldest = CreateListing("Immovlan", "VBE44901", Day1);
        var annotated = CreateListing("Immovlan", "VBE44901", Day1.AddDays(5));
        annotated.PersonalNotes = "À visiter";
        dbContext.PropertyListings.AddRange(oldest, annotated, CreateListing("Immovlan", "VBE44901", Day1.AddDays(9)));
        await dbContext.SaveChangesAsync();

        var removed = await DuplicateListingCleanup.RunAsync(dbContext);

        var kept = Assert.Single(await dbContext.PropertyListings.ToListAsync());
        Assert.Equal(2, removed);
        Assert.Equal(annotated.Id, kept.Id);
        Assert.Equal(Day1, kept.FirstSeenAt);
        Assert.Equal(Day1.AddDays(9), kept.LastSeenAt);
    }

    [Fact]
    public async Task RunAsync_MergesTheSamePropertyAcrossPortals_ButNotIdenticalUnitsOfOnePortal()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var immovlan = CreateListing("Immovlan", "VBE44901", Day1);
        dbContext.PropertyListings.AddRange(
            immovlan,
            CreateListing("Immoweb", "21701625", Day1.AddDays(2)),
            CreateListing("Immoweb", "UNIT-A", Day1, postalCode: "1050", price: 300_000m, area: 80m),
            CreateListing("Immoweb", "UNIT-B", Day1, postalCode: "1050", price: 300_000m, area: 80m));
        await dbContext.SaveChangesAsync();

        var removed = await DuplicateListingCleanup.RunAsync(dbContext);

        var remaining = await dbContext.PropertyListings.Select(l => l.ExternalId).ToListAsync();
        Assert.Equal(1, removed);
        Assert.Equal(["UNIT-A", "UNIT-B", "VBE44901"], remaining.Order());
    }

    [Fact]
    public async Task RunAsync_DoesNothing_WhenThereAreNoDuplicates()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.PropertyListings.AddRange(
            CreateListing("Immovlan", "A", Day1),
            CreateListing("Immoweb", "B", Day1, price: 900_000m));
        await dbContext.SaveChangesAsync();

        Assert.Equal(0, await DuplicateListingCleanup.RunAsync(dbContext));
        Assert.Equal(2, await dbContext.PropertyListings.CountAsync());
    }
}
