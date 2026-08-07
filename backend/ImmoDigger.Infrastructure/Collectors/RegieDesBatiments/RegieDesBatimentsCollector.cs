using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImmoDigger.Infrastructure.Collectors.RegieDesBatiments;

/// <summary>
/// Collects real-estate-for-sale listings from the Regie des Batiments
/// (the federal state's property manager - also handles the sale of
/// former Defense/army sites). A single page
/// ("/fr/venteslocations") server-renders every current sale and rental
/// in one plain HTML list (Drupal Views markup) - no JS rendering, no
/// pagination to walk, no CAPTCHA or anti-bot protection encountered.
/// robots.txt for this host has no Disallow rules at all. One request per
/// collection cycle is enough to see every listing.
/// </summary>
public class RegieDesBatimentsCollector(IHttpClientFactory httpClientFactory, ILogger<RegieDesBatimentsCollector> logger)
    : IListingCollector
{
    public const string HttpClientName = "RegieDesBatiments";

    private const string ListingsPath = "/fr/venteslocations";
    private const string BaseUrl = "https://www.regiedesbatiments.be";

    public string SourceName => "RegieDesBatiments";

    public async Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        string html;
        try
        {
            html = await client.GetStringAsync(ListingsPath, cancellationToken);
        }
        catch (HttpRequestException ex) when (IsBlockedStatus(ex))
        {
            logger.LogWarning(ex, "Regie des Batiments responded with a 403/429 - not retrying.");
            return [];
        }

        var listings = ParseListings(html);
        logger.LogInformation("Regie des Batiments: {Count} for-sale listing(s) found in the target area.", listings.Count);
        return listings;
    }

    private static bool IsBlockedStatus(HttpRequestException ex) =>
        ex.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests;

    /// <summary>Parses every "for sale" row on the ventes/locations page into a <see cref="CollectedListing"/>, skipping rentals and out-of-scope postal codes.</summary>
    internal static List<CollectedListing> ParseListings(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<CollectedListing>();

        var rows = doc.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' view__row ')]");
        if (rows is null)
        {
            return results;
        }

        foreach (var row in rows)
        {
            var listing = ParseRow(row);
            if (listing is not null)
            {
                results.Add(listing);
            }
        }

        return results;
    }

    private static CollectedListing? ParseRow(HtmlNode row)
    {
        var status = row.SelectSingleNode(".//div[contains(@class,'status')]")?.InnerText.Trim();
        if (status is null || !status.Contains("vendre", StringComparison.OrdinalIgnoreCase))
        {
            // Skip rentals ("A louer") and anything without a clear "for sale" status.
            return null;
        }

        var titleLink = row.SelectSingleNode(".//div[contains(@class,'views-field-title')]//a");
        var title = HtmlEntity.DeEntitize(titleLink?.InnerText?.Trim() ?? string.Empty);
        var href = titleLink?.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        var addressParagraphs = row
            .SelectNodes(".//div[contains(@class,'views-field-field-adresse')]//p")
            ?.Select(p => HtmlEntity.DeEntitize(p.InnerText).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("Voir sur la carte", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];

        var street = addressParagraphs.ElementAtOrDefault(0) ?? string.Empty;
        var postalCodeAndCity = addressParagraphs.ElementAtOrDefault(1) ?? string.Empty;
        var postalCodeMatch = Regex.Match(postalCodeAndCity, @"^(\d{4})\s+(.+)$");
        var postalCode = postalCodeMatch.Success ? postalCodeMatch.Groups[1].Value : string.Empty;
        var city = postalCodeMatch.Success ? postalCodeMatch.Groups[2].Value : postalCodeAndCity;

        if (!CollectorConstants.IsInTargetArea(postalCode))
        {
            return null;
        }

        var priceText = row.SelectSingleNode(".//div[contains(@class,'views-field-field-prix-affichage')]//div[@class='field-content']")
            ?.InnerText;
        var askingPrice = ParsePrice(priceText);

        var expirationText = row.SelectSingleNode(".//div[contains(@class,'views-field-field-expiration-date')]//div[@class='field-content']")
            ?.InnerText.Trim();
        var auctionEndDate = ParseExpirationDate(expirationText);

        var imageSrc = row.SelectSingleNode(".//div[contains(@class,'views-field-field-image')]//img")?.GetAttributeValue("src", string.Empty);

        var externalId = href.Trim('/').Replace("fr/buildings/", string.Empty, StringComparison.OrdinalIgnoreCase);
        var url = href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : $"{BaseUrl}{href}";
        var imageUrl = string.IsNullOrWhiteSpace(imageSrc)
            ? null
            : imageSrc.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? imageSrc : $"{BaseUrl}{imageSrc}";

        var description = string.IsNullOrWhiteSpace(expirationText) ? string.Empty : expirationText;

        return new CollectedListing
        {
            Source = "RegieDesBatiments",
            ExternalId = externalId,
            Url = url,
            Title = title,
            ImageUrl = imageUrl,
            Description = description,
            Address = street,
            PostalCode = postalCode,
            City = city,
            AskingPrice = askingPrice,
            SaleType = "PublicSale",
            PropertyType = InferPropertyType(title),
            AuctionEndDate = auctionEndDate,
            RawContentHash = ComputeContentHash(title, askingPrice, street),
        };
    }

    private static decimal? ParsePrice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // "Prix de depart 640.000 euros" - dot as thousands separator, no decimals.
        var match = Regex.Match(text, @"([\d.]+)\s*euros?", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var digitsOnly = match.Groups[1].Value.Replace(".", string.Empty);
        return decimal.TryParse(digitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) ? price : null;
    }

    private static DateTime? ParseExpirationDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = Regex.Match(text, @"(\d{2})/(\d{2})/(\d{4})");
        if (!match.Success)
        {
            return null;
        }

        return DateTime.TryParse(
            $"{match.Groups[3].Value}-{match.Groups[2].Value}-{match.Groups[1].Value}",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    /// <summary>Keyword-based guess from the French title text - this source has no structured property-type field on the listing page.</summary>
    private static string InferPropertyType(string title)
    {
        var lower = title.ToLowerInvariant();

        if (lower.Contains("terrain")) return "Land";
        if (lower.Contains("entrepot") || lower.Contains("hangar")) return "Warehouse";
        if (lower.Contains("bureau")) return "Office";
        if (lower.Contains("appartement")) return "Apartment";
        if (lower.Contains("garage")) return "Garage";
        if (lower.Contains("maison")) return "House";
        if (lower.Contains("immeuble") || lower.Contains("batiment") || lower.Contains("bâtiment") || lower.Contains("caserne")) return "IncomeBuilding";

        return "Other";
    }

    private static string ComputeContentHash(string title, decimal? askingPrice, string address)
    {
        var content = $"{title}|{askingPrice}|{address}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
