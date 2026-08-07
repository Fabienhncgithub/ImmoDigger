using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImmoDigger.Infrastructure.Collectors.Biddit;

/// <summary>
/// Collects real-estate sale listings from Biddit (biddit.be), the online
/// public/private sale platform run by the Belgian notaries' federation
/// (fednot).
///
/// Discovery uses Biddit's own published sitemap (referenced from its
/// robots.txt, which is precisely what sitemaps are for). Listing detail
/// comes from "/api/eco/biddit-bff/lot/{reference}" - a JSON endpoint that
/// Biddit's own web app calls unauthenticated; it was found by reading
/// that public app's JS bundle, not from official third-party API
/// documentation, so it is not guaranteed stable. No CAPTCHA,
/// authentication wall or anti-bot protection was encountered or bypassed;
/// robots.txt does not disallow either path. If Biddit starts responding
/// with 403/429, this collector backs off immediately rather than
/// retrying - see <see cref="FetchLotAsync"/>.
/// </summary>
public class BidditCollector(
    IHttpClientFactory httpClientFactory,
    ILogger<BidditCollector> logger,
    TimeSpan? delayBetweenRequests = null) : IListingCollector
{
    public const string HttpClientName = "Biddit";

    private const string SitemapIndexUrl = "/stg/eco/sitemap_index.xml";

    // Deliberately conservative: this is a personal-use tool checking a
    // handful of communes, not a bulk crawl. Keeping the per-cycle volume
    // and pace low is the actual point of "politeness", not just a rule
    // to satisfy on paper. Tests override the delay (via the constructor)
    // to stay fast without changing the production default.
    private const int MaxListingsPerCycle = 10;
    private readonly TimeSpan _delayBetweenRequests = delayBetweenRequests ?? TimeSpan.FromSeconds(1.5);

    public string SourceName => "Biddit";

    public async Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        var references = await DiscoverListingReferencesAsync(client, cancellationToken);
        logger.LogInformation("Biddit: {Count} listing reference(s) discovered via sitemap.", references.Count);

        var results = new List<CollectedListing>();

        foreach (var reference in references.Take(MaxListingsPerCycle))
        {
            cancellationToken.ThrowIfCancellationRequested();

            BidditLotDto? lot;
            try
            {
                lot = await FetchLotAsync(client, reference, cancellationToken);
            }
            catch (BidditBlockedException)
            {
                logger.LogWarning(
                    "Biddit: received a 403/429 response - stopping this cycle early without retrying, " +
                    "keeping the {Count} listing(s) already collected.", results.Count);
                break;
            }

            if (lot is not null)
            {
                var collected = MapToCollectedListing(lot);
                if (collected is not null)
                {
                    results.Add(collected);
                }
            }

            await Task.Delay(_delayBetweenRequests, cancellationToken);
        }

        return results;
    }

    private async Task<List<string>> DiscoverListingReferencesAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var indexXml = await client.GetStringAsync(SitemapIndexUrl, cancellationToken);
        var sitemapUrls = ParseSitemapIndexUrls(indexXml);

        // A single French-language sitemap is plenty of candidates for a
        // handful of target-area listings per cycle; no need to fetch the
        // Dutch/German ones too and triple the request count for the same
        // underlying properties.
        var frenchSitemap = sitemapUrls.FirstOrDefault(url => url.Contains("fr_sitemap", StringComparison.OrdinalIgnoreCase));
        if (frenchSitemap is null)
        {
            return [];
        }

        var sitemapXml = await client.GetStringAsync(ToRelativePath(frenchSitemap), cancellationToken);
        return ParseSitemapReferences(sitemapXml);
    }

    private async Task<BidditLotDto?> FetchLotAsync(HttpClient client, string reference, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync($"/api/eco/biddit-bff/lot/{reference}", cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && attempt == 1)
            {
                logger.LogWarning(ex, "Biddit: transient error fetching lot {Reference}, retrying once.", reference);
                continue;
            }

            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
            {
                throw new BidditBlockedException();
            }

            if (!response.IsSuccessStatusCode)
            {
                if (attempt == 1)
                {
                    logger.LogWarning(
                        "Biddit: HTTP {Status} fetching lot {Reference}, retrying once.", response.StatusCode, reference);
                    continue;
                }

                logger.LogWarning("Biddit: giving up on lot {Reference} after HTTP {Status}.", reference, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BidditLotDto>(cancellationToken: cancellationToken);
        }

        return null;
    }

    /// <summary>Maps a Biddit lot to ImmoDigger's shape, or null if it falls outside the app's stated scope (Brussels + nearby).</summary>
    internal static CollectedListing? MapToCollectedListing(BidditLotDto lot)
    {
        if (string.IsNullOrWhiteSpace(lot.Reference))
        {
            return null;
        }

        var property = lot.Properties.FirstOrDefault();
        var postalCode = property?.Address?.PostalCode;
        if (!CollectorConstants.IsInTargetArea(postalCode))
        {
            return null;
        }

        var title = FirstNonEmpty(property?.Title?.Fr, property?.Title?.Nl, property?.Title?.En) ?? "Bien immobilier";
        var description = FirstNonEmpty(property?.Description?.Fr, property?.Description?.Nl, property?.Description?.En) ?? string.Empty;
        var street = FirstNonEmpty(property?.Address?.Street?.Fr, property?.Address?.Street?.Nl) ?? string.Empty;
        var municipality = FirstNonEmpty(property?.Address?.Municipality?.Fr, property?.Address?.Municipality?.Nl) ?? string.Empty;
        var address = string.Join(' ', new[] { street, property?.Address?.EstateNumber }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        var image = property?.Pictures.OrderBy(p => p.OrderIndex ?? int.MaxValue).FirstOrDefault();
        var isPublicSale = string.Equals(lot.HandlingMethod, "ONLINE_PUBLIC_SALE", StringComparison.OrdinalIgnoreCase);
        var askingPrice = lot.StartingPrice ?? lot.SellingPrice;

        return new CollectedListing
        {
            Source = "Biddit",
            ExternalId = lot.Reference,
            Url = $"https://www.biddit.be/fr/catalog/detail/{lot.Reference}",
            Title = title,
            ImageUrl = image?.Large ?? image?.Medium,
            Description = description,
            Address = address,
            PostalCode = postalCode ?? string.Empty,
            City = municipality,
            AskingPrice = askingPrice,
            CurrentBid = lot.CurrentPrice,
            SaleType = isPublicSale ? "PublicSale" : "RegularSale",
            PropertyType = MapPropertyType(property?.PropertyType, property?.PropertySubtype),
            BedroomCount = property?.NumberOfBedrooms,
            BathroomCount = property?.NumberOfBathrooms,
            OfficialUnitCount = property?.Construction?.NumberOfHousingUnits,
            LivingArea = property?.LivingSurfaceArea,
            LandArea = property?.Features?.TerrainSurface,
            PebRating = ExtractPebLetter(FirstNonEmpty(property?.EnergeticClassRbc, property?.EnergeticClassRw, property?.EnergeticClassRf)),
            HasGarage = property?.Features?.GarageSurface is > 0,
            HasTerrace = property?.Features?.HasTerrace,
            HasGarden = property?.Features?.HasGarden,
            CadastralIncome = property?.LandIncome?.LandIncome,
            AuctionStartDate = ToUtc(lot.BiddingStartDateTime),
            AuctionEndDate = ToUtc(lot.BiddingEndDateTime),
            PublishedAt = ToUtc(lot.FirstPublicationDateTime),
            RawContentHash = ComputeContentHash(title, description, askingPrice, lot.CurrentPrice, address),
        };
    }

    /// <summary>Best-effort mapping based on the values sampled while building this collector; unmapped values pass through as-is.</summary>
    private static string MapPropertyType(string? propertyType, string? propertySubtype) =>
        propertyType?.ToUpperInvariant() switch
        {
            "HOUSE" => "House",
            "APARTMENT" => "Apartment",
            "LAND" => "Land",
            "GARAGE" => "Garage",
            "BUILDING" or "MIXED_USE" => "IncomeBuilding",
            "INDUSTRIAL" or "COMMERCIAL" => "Warehouse",
            "OFFICE" => "Office",
            _ => propertyType ?? propertySubtype ?? "Other",
        };

    /// <summary>"CLASS_B" -> "B".</summary>
    private static string? ExtractPebLetter(string? energeticClass)
    {
        if (string.IsNullOrWhiteSpace(energeticClass))
        {
            return null;
        }

        var separatorIndex = energeticClass.LastIndexOf('_');
        return separatorIndex >= 0 && separatorIndex < energeticClass.Length - 1
            ? energeticClass[(separatorIndex + 1)..]
            : energeticClass;
    }

    private static string ComputeContentHash(string title, string description, decimal? askingPrice, decimal? currentBid, string address)
    {
        var content = $"{title}|{description}|{askingPrice}|{currentBid}|{address}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

    /// <summary>
    /// Biddit's JSON timestamps carry no time zone offset, so
    /// System.Text.Json deserializes them as Kind=Unspecified - PostgreSQL's
    /// "timestamp with time zone" columns reject anything that isn't
    /// Kind=Utc. The values are already effectively UTC-ish (Biddit is a
    /// Belgian-only system); the small offset this can introduce doesn't
    /// matter for how this app uses these dates (display, rough auction
    /// windows), unlike crashing the whole collection cycle on real data.
    /// </summary>
    private static DateTime? ToUtc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);

    private static string ToRelativePath(string absoluteOrRelativeUrl) =>
        Uri.TryCreate(absoluteOrRelativeUrl, UriKind.Absolute, out var uri) ? uri.PathAndQuery : absoluteOrRelativeUrl;

    internal static List<string> ParseSitemapIndexUrls(string sitemapIndexXml)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return XDocument.Parse(sitemapIndexXml).Descendants(ns + "loc").Select(e => e.Value).ToList();
    }

    internal static List<string> ParseSitemapReferences(string sitemapXml)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var references = new List<string>();

        foreach (var loc in XDocument.Parse(sitemapXml).Descendants(ns + "loc"))
        {
            var match = Regex.Match(loc.Value, @"/catalog/detail/(\d+)\s*$");
            if (match.Success)
            {
                references.Add(match.Groups[1].Value);
            }
        }

        return references.Distinct().ToList();
    }

    /// <summary>Signals a 403/429 response - the caller stops the cycle immediately, without retrying.</summary>
    private sealed class BidditBlockedException : Exception;
}
