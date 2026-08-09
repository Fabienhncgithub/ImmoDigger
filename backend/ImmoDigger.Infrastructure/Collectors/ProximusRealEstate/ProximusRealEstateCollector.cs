using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImmoDigger.Infrastructure.Collectors.ProximusRealEstate;

/// <summary>
/// Collects for-sale listings from proximusrealestate.com ("ConnectImmo"),
/// Proximus's portal for the ~500 former telecom-exchange buildings it is
/// selling off through 2035 (not proximusforrealestate.be, which is a
/// separate B2B fibre-for-developers marketing site with the similarly-
/// worded name - the original seeded URL was wrong and is corrected
/// alongside this collector). robots.txt returns 404 (nothing to
/// disallow); no anti-bot protection was found; no site-specific terms of
/// use prohibiting automated access were found (only Proximus's generic
/// consumer-services legal page, which doesn't apply to this B2B
/// portal). Clean server-rendered HTML throughout, no JS rendering
/// required.
///
/// Two-step, like Biddit/RegieDesBatiments: the search page lists every
/// property (address, postal code, land/building surface, a sale/rent
/// tag) in one request; the asking price only appears on each listing's
/// own detail page, fetched only for listings already in the target area
/// so volume stays naturally small. Content is served in English by
/// default (no locale negotiation attempted) - fine for the structured
/// fields this collector extracts, even though the rest of the app is
/// French-first.
/// </summary>
public partial class ProximusRealEstateCollector(
    IHttpClientFactory httpClientFactory,
    ILogger<ProximusRealEstateCollector> logger,
    TimeSpan? delayBetweenRequests = null) : IListingCollector
{
    public const string HttpClientName = "ProximusRealEstate";

    private const string SearchPath = "/connectimmo/search.html";
    private const string BaseUrl = "https://www.proximusrealestate.com";

    private readonly TimeSpan _delayBetweenRequests = delayBetweenRequests ?? TimeSpan.FromSeconds(1.5);

    public string SourceName => "ProximusRealEstate";

    public async Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        string html;
        try
        {
            html = await client.GetStringAsync(SearchPath, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "ProximusRealEstate: failed to fetch the search page.");
            return [];
        }

        var candidates = ParseSearchResults(html);
        var results = new List<CollectedListing>();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            decimal? price = null;
            try
            {
                var detailHtml = await client.GetStringAsync(candidate.DetailPath, cancellationToken);
                price = ParseSalesPrice(detailHtml);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "ProximusRealEstate: failed to fetch detail page {Path}, keeping the listing without a price.", candidate.DetailPath);
            }

            results.Add(candidate.ToCollectedListing(price));

            await Task.Delay(_delayBetweenRequests, cancellationToken);
        }

        return results;
    }

    /// <summary>A card parsed from the search page - price isn't known until the detail page is fetched, see <see cref="ToCollectedListing"/>.</summary>
    internal sealed record Candidate(
        string ExternalId,
        string DetailPath,
        string Title,
        string Address,
        string PostalCode,
        string City,
        string? ImageUrl,
        decimal? LandArea,
        decimal? LivingArea)
    {
        public CollectedListing ToCollectedListing(decimal? price) => new()
        {
            Source = "ProximusRealEstate",
            ExternalId = ExternalId,
            Url = BaseUrl + DetailPath,
            Title = Title,
            ImageUrl = ImageUrl,
            Address = Address,
            PostalCode = PostalCode,
            City = City,
            AskingPrice = price,
            SaleType = "RegularSale",
            PropertyType = "Other", // the search page doesn't expose a property type/subtype field
            LandArea = LandArea,
            LivingArea = LivingArea,
            RawContentHash = ComputeContentHash(Title, Address, price),
        };
    }

    internal static List<Candidate> ParseSearchResults(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<Candidate>();

        foreach (var article in doc.DocumentNode.SelectNodes("//article[contains(@class,'ci-panel')]") ?? Enumerable.Empty<HtmlNode>())
        {
            // Two "ci-patch" spans can appear per card - an empty one used
            // only as a map-pin label (class "ci-patch ci-map-reference")
            // and the real status badge (class exactly "ci-patch"). The
            // exact-match XPath below only picks the latter.
            var statusText = article.SelectSingleNode(".//span[@class='ci-patch']")?.InnerText?.Trim();
            if (!string.Equals(statusText, "For sale", StringComparison.OrdinalIgnoreCase))
            {
                continue; // skip rentals and any other status
            }

            var link = article.SelectSingleNode(".//a[starts-with(@href,'/details/')]");
            var href = link?.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            var externalId = ExternalIdFromHref().Match(href) is { Success: true } match ? match.Groups[1].Value : null;
            if (string.IsNullOrWhiteSpace(externalId))
            {
                continue;
            }

            var addressText = HtmlEntity.DeEntitize(link!.SelectSingleNode(".//h2")?.InnerText?.Trim()) ?? string.Empty;
            var addressMatch = AddressPattern().Match(addressText);
            if (!addressMatch.Success)
            {
                continue; // address text didn't match "street, postal city" - skip rather than guess
            }

            var postalCode = addressMatch.Groups["postal"].Value;
            if (!CollectorConstants.IsInTargetArea(postalCode))
            {
                continue;
            }

            var image = article.SelectSingleNode(".//img")?.GetAttributeValue("src", string.Empty);

            decimal? landArea = null, livingArea = null;
            foreach (var row in article.SelectNodes(".//div[@class='ci-row']") ?? Enumerable.Empty<HtmlNode>())
            {
                var paragraphs = row.SelectNodes(".//p");
                if (paragraphs is not { Count: >= 2 })
                {
                    continue;
                }

                var label = paragraphs[0].InnerText;
                var valueText = paragraphs[1].InnerText;
                var value = ParseAreaSquareMeters(valueText);

                if (label.Contains("Land", StringComparison.OrdinalIgnoreCase))
                {
                    landArea = value;
                }
                else if (label.Contains("Building", StringComparison.OrdinalIgnoreCase))
                {
                    livingArea = value;
                }
            }

            // The search page has no separate listing title, only the
            // address - used as the title too (e.g. "Bien Proximus - Rue
            // Borrens 16, 1050 Ixelles"), consistent with how a user would
            // recognize this listing at a glance.
            var street = addressMatch.Groups["street"].Value.Trim();
            var city = addressMatch.Groups["city"].Value.Trim();

            results.Add(new Candidate(
                externalId,
                href,
                $"Bien Proximus - {street}, {postalCode} {city}",
                street,
                postalCode,
                city,
                string.IsNullOrWhiteSpace(image) ? null : image,
                landArea,
                livingArea));
        }

        return results;
    }

    /// <summary>"Sales Price:" row from a detail page's definition list - null when the site shows "N/A" (common for negotiated commercial deals).</summary>
    internal static decimal? ParseSalesPrice(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var value = doc.DocumentNode
            .SelectSingleNode("//dt[normalize-space(text())='Sales Price:']/following-sibling::dd[1]")
            ?.InnerText?.Trim();

        if (string.IsNullOrWhiteSpace(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var digitsOnly = NonDigitPattern().Replace(value, string.Empty);
        return decimal.TryParse(digitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) ? price : null;
    }

    private static decimal? ParseAreaSquareMeters(string text)
    {
        var digitsOnly = NonDigitPattern().Replace(text, string.Empty);
        return decimal.TryParse(digitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out var area) ? area : null;
    }

    private static string ComputeContentHash(string title, string address, decimal? price)
    {
        var content = $"{title}|{address}|{price}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    [GeneratedRegex(@"/details/([A-Za-z0-9]+)\.html")]
    private static partial Regex ExternalIdFromHref();

    // e.g. "Rue Borrens 16, 1050 Ixelles / Elsene" -> street="Rue Borrens 16", postal=1050, city="Ixelles"
    [GeneratedRegex(@"^(?<street>.+?),\s*(?<postal>\d{4})\s+(?<city>[^/]+)")]
    private static partial Regex AddressPattern();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex NonDigitPattern();
}
