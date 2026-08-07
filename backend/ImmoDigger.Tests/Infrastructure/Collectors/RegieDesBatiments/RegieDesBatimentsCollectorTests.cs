using System.Net;
using ImmoDigger.Infrastructure.Collectors.RegieDesBatiments;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmoDigger.Tests.Infrastructure.Collectors.RegieDesBatiments;

public class RegieDesBatimentsCollectorTests
{
    private static string ReadFixture() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestData", "RegieDesBatiments", "ventes-locations.html"));

    [Fact]
    public void ParseListings_SkipsRentalRows()
    {
        var listings = RegieDesBatimentsCollector.ParseListings(ReadFixture());

        Assert.DoesNotContain(listings, l => l.Title.Contains("louer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseListings_SkipsListingsOutsideTheTargetArea()
    {
        var listings = RegieDesBatimentsCollector.ParseListings(ReadFixture());

        // The fixture's Namur (5000) row is a for-sale listing but outside
        // Brussels/Brabant - it must not come through.
        Assert.DoesNotContain(listings, l => l.PostalCode == "5000");
    }

    [Fact]
    public void ParseListings_MapsAllExpectedFields_ForAnInScopeForSaleListing()
    {
        var listings = RegieDesBatimentsCollector.ParseListings(ReadFixture());

        var listing = Assert.Single(listings);
        Assert.Equal("RegieDesBatiments", listing.Source);
        Assert.Equal("maison-fictive-vendre-anderlecht-rue-de-test-1", listing.ExternalId);
        Assert.Equal(
            "https://www.regiedesbatiments.be/fr/buildings/maison-fictive-vendre-anderlecht-rue-de-test-1",
            listing.Url);
        Assert.Equal("Maison fictive a vendre a Anderlecht, Rue de Test 1", listing.Title);
        Assert.Equal("Rue de Test 1", listing.Address);
        Assert.Equal("1070", listing.PostalCode);
        Assert.Equal("Anderlecht", listing.City);
        Assert.Equal(275_000m, listing.AskingPrice);
        Assert.Equal("PublicSale", listing.SaleType);
        Assert.Equal("House", listing.PropertyType);
        Assert.Equal(new DateTime(2026, 3, 15), listing.AuctionEndDate);
        Assert.Contains("test-1.jpg", listing.ImageUrl);
        Assert.False(string.IsNullOrWhiteSpace(listing.RawContentHash));
    }

    [Fact]
    public async Task CollectAsync_ReturnsParsedListings_FromTheSinglePage()
    {
        var handler = new FakeHttpMessageHandler().AddTextResponse("venteslocations", ReadFixture(), "text/html");
        var collector = new RegieDesBatimentsCollector(
            new FakeHttpClientFactory("https://www.regiedesbatiments.be", handler),
            NullLogger<RegieDesBatimentsCollector>.Instance);

        var results = await collector.CollectAsync(CancellationToken.None);

        Assert.Single(results);
        Assert.Single(handler.RequestedPaths); // one page, one request
    }

    [Fact]
    public async Task CollectAsync_ReturnsEmpty_WhenBlocked()
    {
        var handler = new FakeHttpMessageHandler().AddStatusResponse("venteslocations", HttpStatusCode.Forbidden);
        var collector = new RegieDesBatimentsCollector(
            new FakeHttpClientFactory("https://www.regiedesbatiments.be", handler),
            NullLogger<RegieDesBatimentsCollector>.Instance);

        var results = await collector.CollectAsync(CancellationToken.None);

        Assert.Empty(results);
    }
}
