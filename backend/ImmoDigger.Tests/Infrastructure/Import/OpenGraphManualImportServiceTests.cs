using System.Net;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Infrastructure.Import;
using ImmoDigger.Tests.Infrastructure.Collectors;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmoDigger.Tests.Infrastructure.Import;

public class OpenGraphManualImportServiceTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "ManualImport", fileName));

    private static OpenGraphManualImportService CreateService(FakeHttpMessageHandler handler) =>
        new(new FakeHttpClientFactory("https://example-test.invalid", handler), NullLogger<OpenGraphManualImportService>.Instance);

    [Fact]
    public async Task ImportFromUrlAsync_BuildsAListing_FromThePagesOpenGraphTags()
    {
        var handler = new FakeHttpMessageHandler().AddTextResponse("listing/1", ReadFixture("og-page.html"), "text/html");
        var sut = CreateService(handler);

        var result = await sut.ImportFromUrlAsync(
            new ImportUrlRequest { Url = "https://example-test.invalid/listing/1" }, CancellationToken.None);

        Assert.False(result.RequiresManualFallback);
        Assert.NotNull(result.Listing);
        Assert.Equal("MAISON FICTIVE DE TEST - IMPORT MANUEL", result.Listing!.Title);
        Assert.Equal("Belle maison fictive utilisee uniquement pour les tests.", result.Listing.Description);
        Assert.Equal("https://example-test.invalid/manual/cover.jpg", result.Listing.ImageUrl);
        Assert.Equal(310_000m, result.Listing.AskingPrice);
        Assert.Equal("ManualImport", result.Listing.Source);
    }

    [Fact]
    public async Task ImportFromUrlAsync_RequiresManualFallback_WhenThePageCannotBeFetched()
    {
        var handler = new FakeHttpMessageHandler().AddStatusResponse("listing/2", HttpStatusCode.Forbidden);
        var sut = CreateService(handler);

        var result = await sut.ImportFromUrlAsync(
            new ImportUrlRequest { Url = "https://example-test.invalid/listing/2" }, CancellationToken.None);

        Assert.True(result.RequiresManualFallback);
        Assert.Null(result.Listing);
    }

    [Fact]
    public async Task ImportFromUrlAsync_UsesTheManualFallback_WithoutFetchingAnything()
    {
        var handler = new FakeHttpMessageHandler(); // no routes registered - any fetch attempt would 404
        var sut = CreateService(handler);

        var result = await sut.ImportFromUrlAsync(
            new ImportUrlRequest
            {
                Url = "https://example-test.invalid/listing/3",
                ManualFallback = new ManualListingFields
                {
                    Title = "Pasted by hand",
                    Price = 200_000m,
                    PostalCode = "1000",
                    City = "Bruxelles",
                    PropertyType = "Apartment",
                },
            },
            CancellationToken.None);

        Assert.False(result.RequiresManualFallback);
        Assert.NotNull(result.Listing);
        Assert.Equal("Pasted by hand", result.Listing!.Title);
        Assert.Equal(200_000m, result.Listing.AskingPrice);
        Assert.Empty(handler.RequestedPaths);
    }
}
