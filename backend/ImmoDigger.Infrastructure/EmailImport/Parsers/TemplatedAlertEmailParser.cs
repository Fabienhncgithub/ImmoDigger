using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>
/// Shared extraction logic for a templated real-estate alert email: find
/// every link that matches the source's listing-detail URL shape, take the
/// surrounding block (table/div) as "this listing's content", and pull
/// title/price/postal code/city/image/property type out of that block with
/// generic heuristics. Only operates on the email content already sitting
/// in the user's own inbox - never fetches anything over the network.
///
/// The URL patterns below are best-effort, built from each site's
/// publicly observable listing-URL shape at the time this was written, not
/// from a real sample of the user's alert emails (which weren't
/// available). They are very likely to need small adjustments (a path
/// segment, an id format) once tried against real alerts - that is
/// expected maintenance for a template-following parser, not a design
/// flaw, and is exactly why each subclass isolates the site-specific bits
/// (name, sender domain, URL pattern) instead of duplicating the
/// extraction logic four times.
/// </summary>
public abstract class TemplatedAlertEmailParser(string sourceName, string senderDomain, Regex listingUrlPattern)
    : IEmailListingParser
{
    public string SourceName { get; } = sourceName;

    public bool CanParse(string senderAddress) =>
        !string.IsNullOrWhiteSpace(senderAddress) &&
        senderAddress.Contains(senderDomain, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyCollection<CollectedListing> Parse(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            return [];
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(message.HtmlBody);

        var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
        if (anchors is null)
        {
            return [];
        }

        var results = new List<CollectedListing>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var anchor in anchors)
        {
            var href = HtmlEntity.DeEntitize(anchor.GetAttributeValue("href", string.Empty));
            var match = listingUrlPattern.Match(href);
            if (!match.Success)
            {
                continue;
            }

            var externalId = match.Groups["id"].Value;
            if (string.IsNullOrWhiteSpace(externalId) || !seenIds.Add(externalId))
            {
                continue; // the same listing is often linked twice (cover image + "view listing" button)
            }

            var block = FindListingBlock(anchor);
            var listing = BuildListing(message, href, externalId, anchor, block);
            if (listing is not null)
            {
                results.Add(listing);
            }
        }

        return results;
    }

    /// <summary>Walks up from the link to the nearest reasonably-sized container, treated as "this listing's own content" (as opposed to the whole email).</summary>
    private static HtmlNode FindListingBlock(HtmlNode anchor) =>
        anchor.Ancestors().FirstOrDefault(a => a.Name is "table" or "div" or "td" && a.InnerText.Trim().Length is > 20 and < 4000)
        ?? anchor.ParentNode
        ?? anchor;

    private CollectedListing? BuildListing(EmailMessage message, string url, string externalId, HtmlNode anchor, HtmlNode block)
    {
        var blockText = HtmlEntity.DeEntitize(block.InnerText) ?? string.Empty;
        var title = ExtractTitle(anchor, block);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var price = ExtractPrice(blockText);
        var (postalCode, city) = ExtractPostalCodeAndCity(blockText);
        var image = block.SelectSingleNode(".//img[@src]")?.GetAttributeValue("src", null);

        return new CollectedListing
        {
            Source = SourceName,
            ExternalId = externalId,
            Url = url,
            Title = title,
            ImageUrl = image,
            Address = string.Empty, // alert emails rarely include a full street address, only postal code/city
            PostalCode = postalCode ?? string.Empty,
            City = city ?? string.Empty,
            AskingPrice = price,
            SaleType = "RegularSale",
            PropertyType = InferPropertyType(blockText),
            PublishedAt = message.ReceivedAt,
            RawContentHash = ComputeContentHash(title, price, externalId, url),
            EmailMessageId = message.MessageId,
            EmailSubject = message.Subject,
            EmailSender = message.Sender,
        };
    }

    private static string? ExtractTitle(HtmlNode anchor, HtmlNode block)
    {
        var anchorText = HtmlEntity.DeEntitize(anchor.InnerText)?.Trim();
        if (!string.IsNullOrWhiteSpace(anchorText) && anchorText.Length > 8 && !LooksLikeGenericLinkText(anchorText))
        {
            return CollapseWhitespace(anchorText);
        }

        var heading = block.SelectSingleNode(".//h1|.//h2|.//h3|.//h4");
        var headingText = heading is null ? null : HtmlEntity.DeEntitize(heading.InnerText)?.Trim();
        return string.IsNullOrWhiteSpace(headingText) ? null : CollapseWhitespace(headingText);
    }

    private static bool LooksLikeGenericLinkText(string text) =>
        Regex.IsMatch(text, @"^(view|voir|bekijk|see|d[ée]tails?|meer|plus)\b", RegexOptions.IgnoreCase);

    private static string CollapseWhitespace(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static decimal? ExtractPrice(string text)
    {
        var match = Regex.Match(text, @"(?:€\s*([\d]{1,3}(?:[.,ʼ]\d{3})*)|([\d]{1,3}(?:[.,ʼ]\d{3})*)\s*€)");
        if (!match.Success)
        {
            return null;
        }

        var raw = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
            .Replace(".", string.Empty).Replace(",", string.Empty).Replace("ʼ", string.Empty);

        return decimal.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) ? price : null;
    }

    private static (string? PostalCode, string? City) ExtractPostalCodeAndCity(string text)
    {
        var match = Regex.Match(text, @"\b(?<postal>[1-9]\d{3})\s+(?<city>[A-ZÀ-Ü][\p{L}'\-]{2,30}(?:[\s-][A-ZÀ-Ü][\p{L}'\-]{2,30})?)\b");
        return match.Success ? (match.Groups["postal"].Value, match.Groups["city"].Value.Trim()) : (null, null);
    }

    /// <summary>Same coarse French/Dutch/English keyword approach as the other collectors - best-effort, unmapped text passes through as "Other".</summary>
    private static string InferPropertyType(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower switch
        {
            _ when ContainsAny(lower, "appartement", "appartment", "flat", "studio") => "Apartment",
            _ when ContainsAny(lower, "maison de rapport", "immeuble de rapport", "opbrengsteigendom") => "IncomeBuilding",
            _ when ContainsAny(lower, "entrepot", "warehouse", "magazijn") => "Warehouse",
            _ when ContainsAny(lower, "maison", "villa", "huis", "woning", "house") => "House",
            _ when ContainsAny(lower, "terrain", "grond", "land") => "Land",
            _ when ContainsAny(lower, "garage", "parking") => "Garage",
            _ when ContainsAny(lower, "bureau", "kantoor", "office") => "Office",
            _ => "Other",
        };
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static string ComputeContentHash(string title, decimal? price, string externalId, string url)
    {
        var content = $"{title}|{price}|{externalId}|{url}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
