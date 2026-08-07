using System.Security.Cryptography;
using System.Text;
using HtmlAgilityPack;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImmoDigger.Infrastructure.Import;

/// <summary>
/// Imports a single listing the user is looking at right now, one URL at a
/// time. Reads only the page's own Open Graph tags (og:title, og:description,
/// og:image, ...) - metadata a page publishes specifically so that other
/// tools (chat apps, this importer) can build a preview of it; fetching it
/// once, on the user's explicit request, is not scraping in the sense the
/// project avoids. If the page can't be fetched or has no usable Open
/// Graph data (most likely for the sites this app deliberately doesn't
/// scrape), nothing is guessed or invented - the caller is told to supply
/// <see cref="ImportUrlRequest.ManualFallback"/> instead.
/// </summary>
public class OpenGraphManualImportService(
    IHttpClientFactory httpClientFactory, ILogger<OpenGraphManualImportService> logger)
    : IManualListingImportService
{
    public const string HttpClientName = "ManualImport";

    public async Task<ManualImportResult> ImportFromUrlAsync(ImportUrlRequest request, CancellationToken cancellationToken)
    {
        if (request.ManualFallback is not null)
        {
            return new ManualImportResult { Listing = BuildFromManualFallback(request.Url, request.ManualFallback) };
        }

        var fetched = await TryFetchOpenGraphAsync(request.Url, cancellationToken);
        return fetched is null
            ? new ManualImportResult { RequiresManualFallback = true }
            : new ManualImportResult { Listing = fetched };
    }

    private async Task<CollectedListing?> TryFetchOpenGraphAsync(string url, CancellationToken cancellationToken)
    {
        string html;
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("Manual import: HTTP {Status} fetching {Url}, falling back to manual entry.", response.StatusCode, url);
                return null;
            }

            html = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogInformation(ex, "Manual import: could not fetch {Url}, falling back to manual entry.", url);
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = MetaContent(doc, "og:title") ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null; // not enough to build a useful listing from
        }

        var description = MetaContent(doc, "og:description") ?? string.Empty;
        var image = MetaContent(doc, "og:image");
        var price = ParsePrice(MetaContent(doc, "product:price:amount") ?? MetaContent(doc, "og:price:amount"));

        return new CollectedListing
        {
            Source = "ManualImport",
            ExternalId = ComputeExternalId(url),
            Url = url,
            Title = HtmlEntity.DeEntitize(title)!.Trim(),
            ImageUrl = image,
            Description = HtmlEntity.DeEntitize(description) ?? string.Empty,
            AskingPrice = price,
            SaleType = "RegularSale",
            PropertyType = "Other",
            RawContentHash = ComputeContentHash(title, description, price),
        };
    }

    private static CollectedListing BuildFromManualFallback(string url, ManualListingFields fields) =>
        new()
        {
            Source = "ManualImport",
            ExternalId = ComputeExternalId(url),
            Url = url,
            Title = fields.Title,
            ImageUrl = fields.ImageUrl,
            Description = fields.Description ?? string.Empty,
            Address = fields.Address ?? string.Empty,
            PostalCode = fields.PostalCode ?? string.Empty,
            City = fields.City ?? string.Empty,
            AskingPrice = fields.Price,
            SaleType = "RegularSale",
            PropertyType = fields.PropertyType ?? "Other",
            RawContentHash = ComputeContentHash(fields.Title, fields.Description ?? string.Empty, fields.Price),
        };

    private static string? MetaContent(HtmlDocument doc, string property) =>
        doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")?.GetAttributeValue("content", null);

    private static decimal? ParsePrice(string? raw) =>
        decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price)
            ? price
            : null;

    /// <summary>Manually-imported listings have no source-assigned id, so a stable hash of the URL stands in (dedup level 1 still applies: re-importing the same URL updates rather than duplicates).</summary>
    private static string ComputeExternalId(string url) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16];

    private static string ComputeContentHash(string title, string description, decimal? price) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{title}|{description}|{price}")));
}
