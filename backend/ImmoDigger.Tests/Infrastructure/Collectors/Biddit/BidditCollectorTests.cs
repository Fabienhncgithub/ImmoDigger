using System.Net;
using System.Text.Json;
using ImmoDigger.Infrastructure.Collectors.Biddit;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmoDigger.Tests.Infrastructure.Collectors.Biddit;

public class BidditCollectorTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "Biddit", fileName));

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ParseSitemapIndexUrls_ExtractsEverySitemapLocation()
    {
        var urls = BidditCollector.ParseSitemapIndexUrls(ReadFixture("sitemap_index.xml"));

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, u => u.Contains("fr_sitemap_1"));
        Assert.Contains(urls, u => u.Contains("nl_sitemap_1"));
    }

    [Fact]
    public void ParseSitemapReferences_ExtractsOnlyListingDetailReferences()
    {
        var references = BidditCollector.ParseSitemapReferences(ReadFixture("fr_sitemap_1.xml"));

        Assert.Equal(["999001", "999002"], references);
    }

    [Fact]
    public void MapToCollectedListing_MapsAllExpectedFields_ForAnInScopeListing()
    {
        var lot = JsonSerializer.Deserialize<BidditLotDto>(ReadFixture("lot-in-scope.json"), JsonOptions)!;

        var result = BidditCollector.MapToCollectedListing(lot);

        Assert.NotNull(result);
        Assert.Equal("Biddit", result!.Source);
        Assert.Equal("999001", result.ExternalId);
        Assert.Equal("https://www.biddit.be/fr/catalog/detail/999001", result.Url);
        Assert.Equal("MAISON DE RAPPORT FICTIVE - TEST", result.Title);
        Assert.Equal("https://example-test.invalid/large.jpg", result.ImageUrl);
        Assert.Equal("Rue de Test 12", result.Address);
        Assert.Equal("1050", result.PostalCode);
        Assert.Equal("Ixelles", result.City);
        Assert.Equal(350_000m, result.AskingPrice);
        Assert.Equal("PublicSale", result.SaleType);
        Assert.Equal("House", result.PropertyType);
        Assert.Equal(4, result.BedroomCount);
        Assert.Equal(2, result.BathroomCount);
        Assert.Equal(3, result.OfficialUnitCount);
        Assert.Equal(280.0m, result.LivingArea);
        Assert.Equal(150.0m, result.LandArea);
        Assert.Equal("E", result.PebRating);
        Assert.True(result.HasGarage);
        Assert.True(result.HasTerrace);
        Assert.False(result.HasGarden);
        Assert.Equal(900.0m, result.CadastralIncome);
        Assert.False(string.IsNullOrWhiteSpace(result.RawContentHash));
    }

    [Fact]
    public void MapToCollectedListing_ReturnsNull_ForAListingOutsideTheTargetArea()
    {
        var lot = JsonSerializer.Deserialize<BidditLotDto>(ReadFixture("lot-out-of-scope.json"), JsonOptions)!;

        var result = BidditCollector.MapToCollectedListing(lot);

        Assert.Null(result);
    }

    [Fact]
    public void MapToCollectedListing_ExtractsEnergyAndCadastralData_FromDescriptionFallbacks()
    {
        var lot = JsonSerializer.Deserialize<BidditLotDto>(ReadFixture("lot-in-scope.json"), JsonOptions)!;
        var property = lot.Properties.Single();
        property.EnergeticClassRbc = null;
        property.PropertyType = "OTHER";
        property.LandIncome = null;
        property.Description!.Fr = "Stedenbouw aanwezig. EPC: Energielabel F - 913 kWh/(m² jaar). KI: 2.247€";

        var result = BidditCollector.MapToCollectedListing(lot);

        Assert.NotNull(result);
        Assert.Equal("F", result!.PebRating);
        Assert.Equal(913m, result.PebConsumption);
        Assert.Equal(2_247m, result.CadastralIncome);
        Assert.Equal("Other", result.PropertyType);
    }

    [Fact]
    public void MapToCollectedListing_MapsCurrentNestedRoomsAndOfficialDocuments()
    {
        var lot = JsonSerializer.Deserialize<BidditLotDto>(ReadFixture("lot-in-scope.json"), JsonOptions)!;
        var property = lot.Properties.Single();
        property.NumberOfBedrooms = null;
        property.NumberOfBathrooms = null;
        property.LivingSurfaceArea = null;
        property.Rooms = new BidditRoomsDto
        {
            NumberOfBedrooms = 6,
            NumberOfBathRooms = 4,
            LivingSurfaceArea = 410m,
        };
        property.Attachments =
        [
            new BidditAttachmentDto
            {
                Type = "URBANISM",
                Name = "Urbanisme.pdf",
                BucketUrl = "https://www.biddit.be/stg/eco/documents/property/urbanisme.pdf",
            },
            new BidditAttachmentDto
            {
                Type = "MISC",
                Name = "Untrusted.pdf",
                BucketUrl = "https://example.com/document.pdf",
            },
        ];

        var result = BidditCollector.MapToCollectedListing(lot);

        Assert.NotNull(result);
        Assert.Equal(6, result!.BedroomCount);
        Assert.Equal(4, result.BathroomCount);
        Assert.Equal(410m, result.LivingArea);
        var document = Assert.Single(result.OfficialDocuments);
        Assert.Equal("Urbanisme.pdf", document.Name);
    }

    [Theory]
    [InlineData("Aantal woongelegenheden: 3", 3)]
    [InlineData("Nombre de logements : 4", 4)]
    [InlineData("Nombre d’unités d’habitation - 2", 2)]
    [InlineData("Anzahl der Wohneinheiten: 5", 5)]
    public void ExtractOfficialUnitCountFromText_RecognizesExplicitMultilingualStatements(string text, int expected)
    {
        Assert.Equal(expected, BidditOfficialDocumentAnalyzer.ExtractOfficialUnitCountFromText(text));
    }

    [Fact]
    public void ExtractOfficialUnitCountFromText_DoesNotGuessFromIncidentalHousingWords()
    {
        const string text = "Stedenbouwkundige vergunning voor het bouwen van een woning.";

        Assert.Null(BidditOfficialDocumentAnalyzer.ExtractOfficialUnitCountFromText(text));
    }

    [Theory]
    [InlineData("Maison actuellement divisée en 3 logements", 3)]
    [InlineData("Pand opgedeeld in 4 wooneenheden", 4)]
    public void ExtractObservedUnitCount_RecognizesExplicitDescriptionStatements(string text, int expected)
    {
        Assert.Equal(expected, BidditCollector.ExtractObservedUnitCount(text));
    }

    [Fact]
    public void IsAllowedDocumentUrl_AcceptsOnlyBidditPublicPdfPath()
    {
        Assert.True(BidditOfficialDocumentAnalyzer.IsAllowedDocumentUrl(
            "https://www.biddit.be/stg/eco/documents/property/urbanisme.pdf"));
        Assert.False(BidditOfficialDocumentAnalyzer.IsAllowedDocumentUrl(
            "https://example.com/stg/eco/documents/property/urbanisme.pdf"));
        Assert.False(BidditOfficialDocumentAnalyzer.IsAllowedDocumentUrl(
            "http://www.biddit.be/stg/eco/documents/property/urbanisme.pdf"));
    }

    private static BidditCollector CreateCollector(FakeHttpMessageHandler handler) =>
        new(
            new FakeHttpClientFactory("https://www.biddit.be", handler),
            NullLogger<BidditCollector>.Instance,
            delayBetweenRequests: TimeSpan.Zero);

    [Fact]
    public async Task CollectAsync_ReturnsOnlyInScopeListings_DiscoveredViaTheSitemap()
    {
        var handler = new FakeHttpMessageHandler()
            .AddTextResponse("sitemap_index.xml", ReadFixture("sitemap_index.xml"), "application/xml")
            .AddTextResponse("fr_sitemap_1.xml", ReadFixture("fr_sitemap_1.xml"), "application/xml")
            .AddJsonResponse("lot/999001", ReadFixture("lot-in-scope.json"))
            .AddJsonResponse("lot/999002", ReadFixture("lot-out-of-scope.json"));

        var results = await CreateCollector(handler).CollectAsync(CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("999001", results.Single().ExternalId);
        Assert.True(
            handler.RequestedPaths.FindIndex(p => p.Contains("999002")) <
            handler.RequestedPaths.FindIndex(p => p.Contains("999001")),
            "The newest sitemap reference should be checked first.");
    }

    [Fact]
    public async Task CollectAsync_StopsImmediately_WhenBidditRespondsWithTooManyRequests()
    {
        var handler = new FakeHttpMessageHandler()
            .AddTextResponse("sitemap_index.xml", ReadFixture("sitemap_index.xml"), "application/xml")
            .AddTextResponse("fr_sitemap_1.xml", ReadFixture("fr_sitemap_1.xml"), "application/xml")
            .AddJsonResponse("lot/999001", ReadFixture("lot-in-scope.json"))
            .AddStatusResponse("lot/999002", HttpStatusCode.TooManyRequests);

        var results = await CreateCollector(handler).CollectAsync(CancellationToken.None);

        Assert.Empty(results);
        // The second lot must never have been requested: a 429 stops the
        // cycle immediately rather than moving on to the next item.
        Assert.DoesNotContain(handler.RequestedPaths, p => p.Contains("999001"));
    }
}
