using System.Net;
using ImmoDigger.Infrastructure.Collectors.ProximusRealEstate;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmoDigger.Tests.Infrastructure.Collectors.ProximusRealEstate;

public class ProximusRealEstateCollectorTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "ProximusRealEstate", fileName));

    [Fact]
    public void ParseSearchResults_KeepsOnlyTheInScopeForSaleCandidate()
    {
        var candidates = ProximusRealEstateCollector.ParseSearchResults(ReadFixture("search.html"));

        var candidate = Assert.Single(candidates);
        Assert.Equal("99TST", candidate.ExternalId);
        Assert.Equal("/details/99TST.html", candidate.DetailPath);
        Assert.Equal("Rue Fictive de Test 42", candidate.Address);
        Assert.Equal("1180", candidate.PostalCode);
        Assert.Equal("Uccle", candidate.City);
        Assert.Equal("https://example-test.invalid/proximus/99TST-cover.jpg", candidate.ImageUrl);
        Assert.Equal(820m, candidate.LandArea);
        Assert.Equal(1450m, candidate.LivingArea);
    }

    [Fact]
    public void ParseSalesPrice_ParsesACommaFormattedPrice()
    {
        var price = ProximusRealEstateCollector.ParseSalesPrice(ReadFixture("detail-99TST.html"));

        Assert.Equal(935_000m, price);
    }

    [Fact]
    public void ParseSalesPrice_ReturnsNull_WhenTheSiteShowsNotApplicable()
    {
        const string html = "<dl><div><dt>Sales Price:</dt><dd>N/A</dd></div></dl>";

        Assert.Null(ProximusRealEstateCollector.ParseSalesPrice(html));
    }

    [Fact]
    public async Task CollectAsync_FetchesTheDetailPage_OnlyForTheInScopeCandidate()
    {
        var handler = new FakeHttpMessageHandler()
            .AddTextResponse("search.html", ReadFixture("search.html"), "text/html")
            .AddTextResponse("99TST.html", ReadFixture("detail-99TST.html"), "text/html");
        var collector = new ProximusRealEstateCollector(
            new FakeHttpClientFactory("https://www.proximusrealestate.com", handler),
            NullLogger<ProximusRealEstateCollector>.Instance,
            delayBetweenRequests: TimeSpan.Zero);

        var results = await collector.CollectAsync(CancellationToken.None);

        var listing = Assert.Single(results);
        Assert.Equal("ProximusRealEstate", listing.Source);
        Assert.Equal("99TST", listing.ExternalId);
        Assert.Equal(935_000m, listing.AskingPrice);
        Assert.Equal(2, handler.RequestedPaths.Count); // search page + exactly one detail page (61OOZ/12REN excluded before ever fetching them)
        Assert.DoesNotContain(handler.RequestedPaths, p => p.Contains("61OOZ") || p.Contains("12REN"));
    }

    [Fact]
    public async Task CollectAsync_ReturnsEmpty_WhenTheSearchPageIsUnreachable()
    {
        var handler = new FakeHttpMessageHandler().AddStatusResponse("search.html", HttpStatusCode.ServiceUnavailable);
        var collector = new ProximusRealEstateCollector(
            new FakeHttpClientFactory("https://www.proximusrealestate.com", handler),
            NullLogger<ProximusRealEstateCollector>.Instance,
            delayBetweenRequests: TimeSpan.Zero);

        var results = await collector.CollectAsync(CancellationToken.None);

        Assert.Empty(results);
    }
}
