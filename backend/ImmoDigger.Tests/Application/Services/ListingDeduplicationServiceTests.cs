using ImmoDigger.Application.Common;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Services;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Tests.Application.Services;

public class ListingDeduplicationServiceTests
{
    private static CollectedListing CreateCollected(
        string source = "Immoweb",
        string externalId = "EXT-1",
        string url = "https://example.invalid/listing/1",
        string title = "Maison de rapport Avenue Coghen",
        string address = "Avenue Coghen",
        string postalCode = "1180",
        string city = "Uccle",
        decimal? askingPrice = 400_000m,
        decimal? livingArea = 320m,
        string rawContentHash = "hash-1") =>
        new()
        {
            Source = source,
            ExternalId = externalId,
            Url = url,
            Title = title,
            Address = address,
            PostalCode = postalCode,
            City = city,
            AskingPrice = askingPrice,
            LivingArea = livingArea,
            SaleType = "RegularSale",
            PropertyType = "IncomeBuilding",
            RawContentHash = rawContentHash,
        };

    [Fact]
    public async Task ProcessAsync_ReturnsNewListing_WhenNothingMatches()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        var outcome = await sut.ProcessAsync(CreateCollected());
        await repository.SaveChangesAsync();

        Assert.Equal(DeduplicationResult.NewListing, outcome.Result);
        Assert.NotNull(outcome.Listing);
        Assert.Equal(1, await dbContext.PropertyListings.CountAsync());
        Assert.Single(await dbContext.ListingPriceHistories.ToListAsync());
    }

    [Fact]
    public async Task ProcessAsync_ReturnsUnchanged_WhenSameSourceAndExternalId_AndNothingChanged()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected());
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected());
        await repository.SaveChangesAsync();

        Assert.Equal(DeduplicationResult.Unchanged, outcome.Result);
        Assert.Equal(1, await dbContext.PropertyListings.CountAsync());
        Assert.Single(await dbContext.ListingPriceHistories.ToListAsync());
    }

    [Fact]
    public async Task ProcessAsync_ReturnsExistingListingUpdated_AndAppendsPriceHistory_WhenPriceChanges()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(askingPrice: 400_000m));
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected(askingPrice: 375_000m, rawContentHash: "hash-1"));
        await repository.SaveChangesAsync();

        Assert.Equal(DeduplicationResult.ExistingListingUpdated, outcome.Result);
        Assert.Equal(375_000m, outcome.Listing!.AskingPrice);
        Assert.Contains(outcome.Reasons, r => r.Contains("Prix modifie", StringComparison.OrdinalIgnoreCase));

        var priceHistory = await dbContext.ListingPriceHistories
            .OrderBy(h => h.RecordedAt)
            .Select(h => h.Price)
            .ToListAsync();
        Assert.Equal([400_000m, 375_000m], priceHistory);
        Assert.Equal(1, await dbContext.PropertyListings.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_ReturnsExistingListingUpdated_WhenContentChangesButPriceDoesNot()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(rawContentHash: "hash-1"));
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected(rawContentHash: "hash-2"));
        await repository.SaveChangesAsync();

        Assert.Equal(DeduplicationResult.ExistingListingUpdated, outcome.Result);
        Assert.Contains(outcome.Reasons, r => r.Contains("Contenu", StringComparison.OrdinalIgnoreCase));

        // No new price point: the asking price itself did not change.
        Assert.Single(await dbContext.ListingPriceHistories.ToListAsync());
    }

    [Fact]
    public async Task ProcessAsync_MatchesByNormalizedUrl_WhenExternalIdChanged()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(externalId: "EXT-OLD", url: "https://example.invalid/listing/1"));
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected(externalId: "EXT-NEW", url: "https://example.invalid/listing/1/"));
        await repository.SaveChangesAsync();

        Assert.Equal(DeduplicationResult.ExistingListingUpdated, outcome.Result);
        Assert.Equal(1, await dbContext.PropertyListings.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_FlagsProbableDuplicate_WhenAddressTitlePriceAndAreaAllMatch_ButSourceDiffers()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(source: "Immoweb", externalId: "IMMOWEB-1"));
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected(
            source: "Immovlan",
            externalId: "IMMOVLAN-1",
            url: "https://example.invalid/listing/1-on-immovlan",
            askingPrice: 405_000m, // within 2% tolerance of 400 000
            livingArea: 325m, // within 5% tolerance of 320
            rawContentHash: "hash-immovlan-mirror")); // deliberately different: exercises the address+title/price/area path, not the content-hash shortcut

        Assert.Equal(DeduplicationResult.ProbableDuplicate, outcome.Result);
        Assert.Null(outcome.Listing);
        Assert.NotEmpty(outcome.Reasons);

        // Nothing was written for the probable duplicate.
        Assert.Equal(1, await dbContext.PropertyListings.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_DoesNotFlagDuplicate_WhenAddressMatches_ButTitleAndPriceDoNot()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(source: "Immoweb", externalId: "IMMOWEB-1"));
        await repository.SaveChangesAsync();

        // Same building, but a completely different listing (different unit,
        // different price bracket, different title, different content) -
        // must NOT be flagged.
        var outcome = await sut.ProcessAsync(CreateCollected(
            source: "Immovlan",
            externalId: "IMMOVLAN-1",
            url: "https://example.invalid/listing/other-unit",
            title: "Studio meuble centre ville",
            askingPrice: 950_000m,
            livingArea: 45m,
            rawContentHash: "hash-other-unit"));
        await repository.SaveChangesAsync();

        Assert.Equal(DeduplicationResult.NewListing, outcome.Result);
        Assert.Equal(2, await dbContext.PropertyListings.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_FlagsProbableDuplicate_WhenContentHashMatches_EvenAtADifferentAddress()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(rawContentHash: "shared-hash"));
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected(
            source: "Zimmo",
            externalId: "ZIMMO-1",
            url: "https://example.invalid/listing/mirrored",
            address: "Rue Completement Differente",
            postalCode: "1000",
            city: "Bruxelles",
            rawContentHash: "shared-hash"));

        Assert.Equal(DeduplicationResult.ProbableDuplicate, outcome.Result);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotInsertTwice_WhenTheSameListingComesTwiceInOneUnsavedBatch()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        // Same listing in two alert emails: each email links through its own
        // tracking URL and yields its own content hash, and nothing is saved
        // in between - exactly what a collection cycle does.
        var first = await sut.ProcessAsync(CreateCollected(
            source: "Immovlan", externalId: "VBE44901", url: "https://example.invalid/tr/aaa", rawContentHash: "hash-a"));
        var second = await sut.ProcessAsync(CreateCollected(
            source: "Immovlan", externalId: "VBE44901", url: "https://example.invalid/tr/bbb", rawContentHash: "hash-b"));
        await repository.SaveChangesAsync();

        Assert.Equal(DeduplicationResult.NewListing, first.Result);
        Assert.NotEqual(DeduplicationResult.NewListing, second.Result);
        Assert.Equal(1, await dbContext.PropertyListings.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_FlagsProbableDuplicate_WhenAnotherPortalHasTheSamePostalCodePriceAndArea()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(
            source: "Immovlan", externalId: "VBE44901", title: "Immeuble de rapport à vendre à Bruxelles, 1000",
            address: "", postalCode: "1000", city: "Bruxelles", askingPrice: 1_440_000m, livingArea: 901m));
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected(
            source: "Immoweb", externalId: "21701625", url: "https://example.invalid/listing/2",
            title: "1 500 000 € 1 440 000 € 3 ch. • 901 m² 1000 Bruxelles Voir +",
            address: "", postalCode: "1000", city: "Bruxelles Voir", askingPrice: 1_440_000m, livingArea: 901m,
            rawContentHash: "hash-2"));

        Assert.Equal(DeduplicationResult.ProbableDuplicate, outcome.Result);
    }

    [Fact]
    public async Task ProcessAsync_KeepsBothListings_WhenTheSamePortalHasTwoIdenticalUnits()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var repository = new PropertyListingRepository(dbContext);
        var sut = new ListingDeduplicationService(repository);

        await sut.ProcessAsync(CreateCollected(
            externalId: "UNIT-A", title: "Appartement A1", address: "", askingPrice: 300_000m, livingArea: 80m));
        await repository.SaveChangesAsync();

        var outcome = await sut.ProcessAsync(CreateCollected(
            externalId: "UNIT-B", url: "https://example.invalid/listing/2", title: "Studio B2 rénové", address: "",
            askingPrice: 300_000m, livingArea: 80m, rawContentHash: "hash-2"));

        Assert.Equal(DeduplicationResult.NewListing, outcome.Result);
    }
}
