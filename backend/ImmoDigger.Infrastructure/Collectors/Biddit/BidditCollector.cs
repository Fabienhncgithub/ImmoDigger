using System.Net;
using System.Net.Http.Json;
using System.Globalization;
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
    private const int MaxCandidatesPerCycle = 20;
    private readonly TimeSpan _delayBetweenRequests = delayBetweenRequests ?? TimeSpan.FromMilliseconds(750);

    public string SourceName => "Biddit";

    public async Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        var references = await DiscoverListingReferencesAsync(client, cancellationToken);
        logger.LogInformation("Biddit: {Count} listing reference(s) discovered via sitemap.", references.Count);

        var results = new List<CollectedListing>();

        // Biddit orders detail references from oldest to newest. Looking at
        // the first entries repeatedly meant examining stale lots and could
        // miss every listing around Brussels. Start with the newest lots and
        // inspect a bounded candidate window; stop as soon as the accepted
        // result limit is reached.
        foreach (var reference in references.AsEnumerable().Reverse().Take(MaxCandidatesPerCycle))
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
                    collected = await EnrichFromOfficialDocumentsAsync(
                        client,
                        lot,
                        collected,
                        cancellationToken);
                    results.Add(collected);
                    if (results.Count >= MaxListingsPerCycle)
                    {
                        break;
                    }
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
        var pebRating = ExtractPebLetter(FirstNonEmpty(
            property?.EnergeticClassRbc,
            property?.EnergeticClassRw,
            property?.EnergeticClassRf)) ?? ExtractPebLetterFromDescription(description);
        var pebConsumption = ExtractPebConsumption(description);
        var cadastralIncome = property?.LandIncome?.LandIncome ?? ExtractCadastralIncome(description);
        var listingUrl = $"https://www.biddit.be/fr/catalog/detail/{lot.Reference}";
        var officialUnitCount = property?.Construction?.NumberOfHousingUnits;
        var propertyType = MapPropertyType(property?.PropertyType, property?.PropertySubtype);
        var observedUnitCount = ExtractObservedUnitCount(description) ??
                                (propertyType is "House" or "Apartment" ? 1 : null);
        var officialDocuments = (property?.Attachments ?? [])
            .Where(attachment => BidditOfficialDocumentAnalyzer.IsAllowedDocumentUrl(attachment.BucketUrl))
            .Select(attachment => new ListingDocumentDto(
                attachment.Type ?? "OTHER",
                string.IsNullOrWhiteSpace(attachment.Name) ? "Document officiel" : attachment.Name,
                attachment.BucketUrl!))
            .DistinctBy(document => document.Url, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var documentSignature = string.Join('|', officialDocuments.Select(document => $"{document.Type}:{document.Name}:{document.Url}"));

        return new CollectedListing
        {
            Source = "Biddit",
            ExternalId = lot.Reference,
            Url = listingUrl,
            Title = title,
            ImageUrl = image?.Large ?? image?.Medium,
            Description = description,
            Address = address,
            PostalCode = postalCode ?? string.Empty,
            City = municipality,
            AskingPrice = askingPrice,
            CurrentBid = lot.CurrentPrice,
            SaleType = isPublicSale ? "PublicSale" : "RegularSale",
            PropertyType = propertyType,
            BedroomCount = property?.Rooms?.NumberOfBedrooms ?? property?.NumberOfBedrooms,
            BathroomCount = property?.Rooms?.NumberOfBathRooms ?? property?.NumberOfBathrooms,
            OfficialUnitCount = officialUnitCount,
            OfficialUnitCountSourceName = officialUnitCount.HasValue ? "Fiche structurée Biddit" : null,
            OfficialUnitCountSourceUrl = officialUnitCount.HasValue ? listingUrl : null,
            ObservedUnitCount = observedUnitCount,
            OfficialDocuments = officialDocuments,
            LivingArea = property?.Rooms?.LivingSurfaceArea ?? property?.LivingSurfaceArea,
            LandArea = property?.Features?.TerrainSurface,
            PebRating = pebRating,
            PebConsumption = pebConsumption,
            HasGarage = property?.Features?.GarageSurface is > 0,
            HasTerrace = property?.Features?.HasTerrace,
            HasGarden = property?.Features?.HasGarden,
            CadastralIncome = cadastralIncome,
            AuctionStartDate = ToUtc(lot.BiddingStartDateTime),
            AuctionEndDate = ToUtc(lot.BiddingEndDateTime),
            PublishedAt = ToUtc(lot.FirstPublicationDateTime),
            RawContentHash = ComputeContentHash(
                title,
                description,
                askingPrice,
                lot.CurrentPrice,
                address,
                pebRating,
                pebConsumption,
                cadastralIncome,
                officialUnitCount,
                documentSignature),
        };
    }

    private async Task<CollectedListing> EnrichFromOfficialDocumentsAsync(
        HttpClient client,
        BidditLotDto lot,
        CollectedListing collected,
        CancellationToken cancellationToken)
    {
        if (collected.OfficialUnitCount.HasValue)
        {
            return collected;
        }

        var attachments = lot.Properties.FirstOrDefault()?.Attachments ?? [];
        var evidence = await BidditOfficialDocumentAnalyzer.FindOfficialUnitCountAsync(
            client,
            attachments,
            logger,
            cancellationToken);

        return evidence is null
            ? collected
            : collected with
            {
                OfficialUnitCount = evidence.Count,
                OfficialUnitCountSourceName = $"Document urbanistique Biddit · {evidence.DocumentName}",
                OfficialUnitCountSourceUrl = evidence.DocumentUrl,
                RawContentHash = ComputeEnrichedContentHash(
                    collected.RawContentHash,
                    evidence.Count,
                    evidence.DocumentUrl),
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
            "OTHER" => "Other",
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

    private static string? ExtractPebLetterFromDescription(string description)
    {
        var match = Regex.Match(
            description,
            @"(?is)\b(?:EPC|PEB|ENERGIELABEL|LABEL\s+ÉNERGÉTIQUE|CLASSE\s+ÉNERGÉTIQUE)\b.{0,240}?\b([A-G])(?:\+{1,2})?\b");
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static decimal? ExtractPebConsumption(string description)
    {
        var match = Regex.Match(
            description,
            @"(?i)\b(\d{2,4}(?:[.,]\d+)?)\s*kWh\s*/\s*\(?m(?:²|2)");
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(
            match.Groups[1].Value.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
    }

    private static decimal? ExtractCadastralIncome(string description)
    {
        var match = Regex.Match(description, @"(?i)\bKI\s*:\s*([\d.\s]+)");
        if (!match.Success)
        {
            return null;
        }

        var digits = Regex.Replace(match.Groups[1].Value, @"\D", string.Empty);
        return decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    /// <summary>
    /// Extracts only an explicit residential-unit statement from the public
    /// description. For a plain house/apartment the caller uses one advertised
    /// unit as a fallback, while keeping it distinct from the official count.
    /// </summary>
    internal static int? ExtractObservedUnitCount(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var patterns = new[]
        {
            @"(?i)\b(?:divis(?:é|e)e?|compos(?:é|e)e?)\s+(?:sans\s+autorisation\s+)?(?:en|de)\s+(\d{1,2})\s+logements?\b",
            @"(?i)\b(\d{1,2})\s+(?:logements?|appartements?|unités?\s+d['’]habitation)\b",
            @"(?i)\b(?:verdeeld|opgedeeld)\s+in\s+(\d{1,2})\s+(?:wooneenheden|woongelegenheden|woningen|appartementen)\b",
            @"(?i)\b(\d{1,2})\s+(?:wooneenheden|woongelegenheden|woningen|appartementen)\b",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(description, pattern);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var count) && count is > 0 and <= 50)
            {
                return count;
            }
        }

        return null;
    }

    private static string ComputeContentHash(
        string title,
        string description,
        decimal? askingPrice,
        decimal? currentBid,
        string address,
        string? pebRating,
        decimal? pebConsumption,
        decimal? cadastralIncome,
        int? officialUnitCount,
        string documentSignature)
    {
        var content = $"{title}|{description}|{askingPrice}|{currentBid}|{address}|{pebRating}|{pebConsumption}|{cadastralIncome}|{officialUnitCount}|{documentSignature}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    private static string ComputeEnrichedContentHash(string baseHash, int officialUnitCount, string sourceUrl)
    {
        var content = $"{baseHash}|{officialUnitCount}|{sourceUrl}";
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
