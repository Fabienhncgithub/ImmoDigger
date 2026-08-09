using System.Net;
using ImmoDigger.Infrastructure.Collectors.BpostImmo;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmoDigger.Tests.Infrastructure.Collectors.BpostImmo;

public class BpostImmoCollectorTests
{
    private static string ReadFixture() => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestData", "BpostImmo", "a-vendre.html"));

    [Fact]
    public void ParseListings_MapsTheInScopeForSaleListing_WithAllExpectedFields()
    {
        var listings = BpostImmoCollector.ParseListings(ReadFixture());

        var listing = Assert.Single(listings);
        Assert.Equal("BpostImmo", listing.Source);
        Assert.Equal("9990001", listing.ExternalId);
        Assert.Equal(
            "https://bpostimmo.be/fr/offre/9990001/batiment-fictif-a-vendre-a-1050-ixelles", listing.Url);
        Assert.Equal("BATIMENT FICTIF DE TEST A VENDRE", listing.Title);
        Assert.Equal("https://example-test.invalid/bpost/9990001-cover.jpg", listing.ImageUrl);
        Assert.Equal("Rue de Test 12", listing.Address);
        Assert.Equal("1050", listing.PostalCode);
        Assert.Equal("Ixelles", listing.City);
        Assert.Equal(610_000m, listing.AskingPrice);
        Assert.Equal("RegularSale", listing.SaleType);
        Assert.Equal("Office", listing.PropertyType); // "Batiment mixte" contains neither -> falls through to the "bureau/mixte" branch
        Assert.Equal(420m, listing.LivingArea);
        Assert.Equal(150m, listing.LandArea);
        Assert.Equal("E", listing.PebRating);
        Assert.False(string.IsNullOrWhiteSpace(listing.RawContentHash));
    }

    [Fact]
    public void ParseListings_SkipsListingsOutsideTheTargetArea()
    {
        var listings = BpostImmoCollector.ParseListings(ReadFixture());

        Assert.DoesNotContain(listings, l => l.PostalCode == "6040");
    }

    [Fact]
    public void ParseListings_SkipsRentals()
    {
        var listings = BpostImmoCollector.ParseListings(ReadFixture());

        Assert.DoesNotContain(listings, l => l.PostalCode == "1000");
    }

    [Fact]
    public void ExtractBalancedArray_HandlesBracketsInsideStringValues()
    {
        const string html = "app.set(\"estateGroups\", [{\"a\":\"text with [brackets] inside\"}]);";

        var json = BpostImmoCollector.ExtractBalancedArray(html, "app.set(\"estateGroups\", ");

        Assert.Equal("[{\"a\":\"text with [brackets] inside\"}]", json);
    }

    [Fact]
    public async Task CollectAsync_ReturnsTheParsedListings_FromASinglePageFetch()
    {
        var handler = new FakeHttpMessageHandler().AddTextResponse("a-vendre", ReadFixture(), "text/html");
        var collector = new BpostImmoCollector(
            new FakeHttpClientFactory("https://bpostimmo.be", handler), NullLogger<BpostImmoCollector>.Instance);

        var results = await collector.CollectAsync(CancellationToken.None);

        Assert.Single(results);
        Assert.Single(handler.RequestedPaths); // one page, one request - no separate detail fetches needed
    }

    [Fact]
    public async Task CollectAsync_ReturnsEmpty_WhenTheSiteIsUnreachable()
    {
        var handler = new FakeHttpMessageHandler().AddStatusResponse("a-vendre", HttpStatusCode.ServiceUnavailable);
        var collector = new BpostImmoCollector(
            new FakeHttpClientFactory("https://bpostimmo.be", handler), NullLogger<BpostImmoCollector>.Instance);

        var results = await collector.CollectAsync(CancellationToken.None);

        Assert.Empty(results);
    }
}
