using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>
/// Shared extraction logic for official saved-search alert emails. It only
/// reads mail already delivered to the user's mailbox; it never crawls or
/// fetches the portal linked by the alert.
/// </summary>
public abstract class TemplatedAlertEmailParser : IEmailListingParser
{
    private const int MaximumDescriptionLength = 4_000;
    private static readonly Regex AbsoluteUrlPattern = new(
        @"https?://[^\s<>""']+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

    private readonly IReadOnlyCollection<string> _senderDomains;
    private readonly Regex _listingUrlPattern;

    protected TemplatedAlertEmailParser(string sourceName, string senderDomain, Regex listingUrlPattern)
        : this(sourceName, [senderDomain], listingUrlPattern)
    {
    }

    protected TemplatedAlertEmailParser(
        string sourceName,
        IReadOnlyCollection<string> senderDomains,
        Regex listingUrlPattern)
    {
        SourceName = sourceName;
        _senderDomains = senderDomains;
        _listingUrlPattern = listingUrlPattern;
    }

    public string SourceName { get; }

    public bool CanParse(string senderAddress)
    {
        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            return false;
        }

        var at = senderAddress.LastIndexOf('@');
        if (at < 0 || at == senderAddress.Length - 1)
        {
            return false;
        }

        var actualDomain = senderAddress[(at + 1)..].Trim().TrimEnd('.');
        return _senderDomains.Any(allowedDomain =>
            actualDomain.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) ||
            actualDomain.EndsWith($".{allowedDomain}", StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyCollection<CollectedListing> Parse(EmailMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            return ParseHtmlBody(message);
        }

        return string.IsNullOrWhiteSpace(message.TextBody) ? [] : ParseTextBody(message);
    }

    private IReadOnlyCollection<CollectedListing> ParseHtmlBody(EmailMessage message)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(message.HtmlBody!);

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
            var block = FindListingBlock(anchor);
            var matchedDirectly = TryMatchListingUrl(href, out var listingUrl, out var externalId);
            if (!matchedDirectly && !TryMatchEmbeddedListing(anchor, block, href, out listingUrl, out externalId))
            {
                continue;
            }

            if (!seenIds.Add(externalId))
            {
                continue;
            }

            var blockText = HtmlEntity.DeEntitize(block.InnerText) ?? string.Empty;
            var title = ExtractTitle(anchor, block);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = FallbackTitle(message, externalId);
            }

            var image = ExtractImageUrl(block);
            results.Add(BuildListing(message, listingUrl, externalId, title, blockText, image));
        }

        return results;
    }

    /// <summary>
    /// Source-specific fallback for alert templates that expose their stable
    /// listing id inside the delivered card but wrap every clickable URL in a
    /// tracking link. The default deliberately accepts nothing.
    /// </summary>
    protected virtual bool TryMatchEmbeddedListing(
        HtmlNode anchor,
        HtmlNode block,
        string href,
        out string listingUrl,
        out string externalId)
    {
        listingUrl = string.Empty;
        externalId = string.Empty;
        return false;
    }

    private IReadOnlyCollection<CollectedListing> ParseTextBody(EmailMessage message)
    {
        var body = WebUtility.HtmlDecode(message.TextBody ?? string.Empty);
        var results = new List<CollectedListing>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match urlMatch in AbsoluteUrlPattern.Matches(body))
        {
            var rawUrl = urlMatch.Value.TrimEnd('.', ',', ';', ')', ']');
            if (!TryMatchListingUrl(rawUrl, out var listingUrl, out var externalId) || !seenIds.Add(externalId))
            {
                continue;
            }

            var contextStart = Math.Max(0, urlMatch.Index - 600);
            var contextLength = Math.Min(body.Length - contextStart, urlMatch.Length + 1_200);
            var context = body.Substring(contextStart, contextLength);
            var title = ExtractTextTitle(body, urlMatch.Index) ?? FallbackTitle(message, externalId);
            results.Add(BuildListing(message, listingUrl, externalId, title, context, image: null));
        }

        return results;
    }

    private bool TryMatchListingUrl(string href, out string listingUrl, out string externalId)
    {
        foreach (var candidate in ExpandUrlCandidates(href))
        {
            var match = _listingUrlPattern.Match(candidate);
            if (!match.Success || string.IsNullOrWhiteSpace(match.Groups["id"].Value))
            {
                continue;
            }

            listingUrl = ExtractMatchedUrl(candidate, match.Index);
            externalId = match.Groups["id"].Value;
            return true;
        }

        listingUrl = string.Empty;
        externalId = string.Empty;
        return false;
    }

    private static string ExtractMatchedUrl(string candidate, int portalMatchIndex)
    {
        // When a decoded tracking URL contains the destination as a query
        // value, keep the destination rather than persisting the tracker.
        var httpsStart = candidate.LastIndexOf("https://", portalMatchIndex, StringComparison.OrdinalIgnoreCase);
        var httpStart = candidate.LastIndexOf("http://", portalMatchIndex, StringComparison.OrdinalIgnoreCase);
        var start = Math.Max(httpsStart, httpStart);
        return start >= 0 ? candidate[start..] : candidate;
    }

    /// <summary>
    /// Alert providers often wrap the real listing URL in a click-tracking
    /// URL. Inspecting and URL-decoding its query values is enough to recover
    /// the already-delivered destination without following the redirect.
    /// </summary>
    private static IEnumerable<string> ExpandUrlCandidates(string href)
    {
        var queue = new Queue<(string Value, int Depth)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        queue.Enqueue((WebUtility.HtmlDecode(href).Trim(), 0));

        while (queue.Count > 0)
        {
            var (value, depth) = queue.Dequeue();
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
            {
                continue;
            }

            yield return value;
            if (depth >= 2)
            {
                continue;
            }

            var decoded = SafeUrlDecode(value);
            if (!string.Equals(decoded, value, StringComparison.Ordinal))
            {
                queue.Enqueue((decoded, depth + 1));
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Query))
            {
                continue;
            }

            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var equals = pair.IndexOf('=');
                if (equals < 0 || equals == pair.Length - 1)
                {
                    continue;
                }

                queue.Enqueue((SafeUrlDecode(pair[(equals + 1)..]), depth + 1));
            }
        }
    }

    private static string SafeUrlDecode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        catch (UriFormatException)
        {
            return value;
        }
    }

    protected static HtmlNode FindListingBlock(HtmlNode anchor) =>
        anchor.Ancestors().FirstOrDefault(HasCardClass)
        ?? anchor.Ancestors().FirstOrDefault(a =>
            a.Name is "table" or "div" or "td" && a.InnerText.Trim().Length is > 20 and < 4_000)
        ?? anchor.ParentNode
        ?? anchor;

    private static bool HasCardClass(HtmlNode node) =>
        node.GetAttributeValue("class", string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains("card", StringComparer.OrdinalIgnoreCase);

    private CollectedListing BuildListing(
        EmailMessage message,
        string url,
        string externalId,
        string title,
        string rawBlockText,
        string? image)
    {
        var blockText = CollapseWhitespace(WebUtility.HtmlDecode(rawBlockText));
        var (postalCode, city) = ExtractPostalCodeAndCity(blockText);
        var price = ExtractPrice(blockText);
        var livingArea = ExtractArea(blockText, "surface habitable", "bewoonbare oppervlakte", "living area");
        var landArea = ExtractArea(blockText, "surface du terrain", "grondoppervlakte", "land area");
        livingArea ??= ExtractFirstArea(blockText);

        var description = blockText.Length <= MaximumDescriptionLength
            ? blockText
            : blockText[..MaximumDescriptionLength];

        return new CollectedListing
        {
            Source = SourceName,
            ExternalId = externalId,
            Url = url,
            Title = CollapseWhitespace(title),
            ImageUrl = string.IsNullOrWhiteSpace(image) ? null : WebUtility.HtmlDecode(image),
            Description = description,
            Address = string.Empty,
            PostalCode = postalCode ?? string.Empty,
            City = city ?? string.Empty,
            AskingPrice = price,
            SaleType = "RegularSale",
            PropertyType = InferPropertyType(blockText),
            BedroomCount = ExtractInteger(blockText, "chambre", "slaapkamer", "bedroom"),
            BathroomCount = ExtractInteger(blockText, "salle de bain", "badkamer", "bathroom"),
            LivingArea = livingArea,
            LandArea = landArea,
            PebRating = ExtractEnergyRating(blockText),
            PublishedAt = message.ReceivedAt,
            RawContentHash = ComputeContentHash(title, description, price, externalId, url),
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
        if (!string.IsNullOrWhiteSpace(headingText))
        {
            return CollapseWhitespace(headingText);
        }

        return block.SelectNodes(".//a")?
            .Select(candidate => HtmlEntity.DeEntitize(candidate.InnerText)?.Trim())
            .FirstOrDefault(candidate =>
                !string.IsNullOrWhiteSpace(candidate) &&
                candidate.Length > 8 &&
                !LooksLikeGenericLinkText(candidate)) is { } alternative
            ? CollapseWhitespace(alternative)
            : null;
    }

    private static string? ExtractImageUrl(HtmlNode block)
    {
        var image = block.SelectSingleNode(".//img[@src]")?.GetAttributeValue("src", string.Empty);
        if (!string.IsNullOrWhiteSpace(image))
        {
            return HtmlEntity.DeEntitize(image);
        }

        var html = HtmlEntity.DeEntitize(block.OuterHtml);
        var backgroundImage = Regex.Match(
            html,
            @"background-image\s*:\s*url\(\s*['""']?(?<url>https?://[^'""\)\s]+)",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        return backgroundImage.Success ? backgroundImage.Groups["url"].Value : null;
    }

    private static string? ExtractTextTitle(string body, int urlIndex)
    {
        var before = body[..urlIndex];
        return before.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => CollapseWhitespace(line))
            .LastOrDefault(line => line.Length is > 8 and <= 200 && !LooksLikeGenericLinkText(line));
    }

    private string FallbackTitle(EmailMessage message, string externalId) =>
        string.IsNullOrWhiteSpace(message.Subject)
            ? $"Annonce {SourceName} {externalId}"
            : $"{message.Subject} ({externalId})";

    private static bool LooksLikeGenericLinkText(string text) =>
        Regex.IsMatch(text, @"^(view|voir|bekijk|see|d[ée]tails?|meer|plus|ouvrir|consulter)\b", RegexOptions.IgnoreCase);

    private static string CollapseWhitespace(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static decimal? ExtractPrice(string text)
    {
        const string amount = @"(?<amount>\d{1,3}(?:[.\s\u00A0\u202F,'’ʼ]\d{3})+|\d{4,9})";
        // Prefer an amount following the currency symbol. Otherwise a card
        // such as "Bruxelles, 1000 € 2.400.000" is incorrectly interpreted
        // as a €1,000 price because regex matching starts at the postal code.
        var match = Regex.Match(
            text,
            $@"€\s*{amount}",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        if (!match.Success)
        {
            match = Regex.Match(
                text,
                $@"{amount}\s*(?:€|EUR)",
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));
        }

        if (!match.Success)
        {
            return null;
        }

        var digits = Regex.Replace(match.Groups["amount"].Value, @"\D", string.Empty);
        return decimal.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) ? price : null;
    }

    private static (string? PostalCode, string? City) ExtractPostalCodeAndCity(string text)
    {
        var match = Regex.Match(
            text,
            @"\b(?<postal>[1-9]\d{3})[\s,]+(?<city>[\p{L}][\p{L}'’.\-]*(?:[\s-]+[\p{L}][\p{L}'’.\-]*){0,4})",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        if (match.Success)
        {
            return (match.Groups["postal"].Value, match.Groups["city"].Value.Trim());
        }

        // Immovlan's current alert cards use "à Laeken, 1020" rather than
        // the more common "1020 Laeken" ordering.
        var reversed = Regex.Match(
            text,
            @"\b(?:à|te|in)\s+(?<city>[\p{L}][\p{L}'’.\-]*(?:[\s-]+(?!(?:à|te|in)\b)[\p{L}][\p{L}'’.\-]*){0,3})\s*,\s*(?<postal>[1-9]\d{3})\b",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        return reversed.Success
            ? (reversed.Groups["postal"].Value, reversed.Groups["city"].Value.Trim())
            : (null, null);
    }

    private static int? ExtractInteger(string text, params string[] labels)
    {
        var alternation = string.Join('|', labels.Select(Regex.Escape));
        var match = Regex.Match(
            text,
            $@"(?:(?<before>\d{{1,2}})\s*(?:{alternation})s?\b|(?:{alternation})s?\s*[:\-]?\s*(?<after>\d{{1,2}}))",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        var value = match.Groups["before"].Success ? match.Groups["before"].Value : match.Groups["after"].Value;
        return int.TryParse(value, out var parsed) && parsed is > 0 and <= 99 ? parsed : null;
    }

    private static decimal? ExtractArea(string text, params string[] labels)
    {
        foreach (var label in labels)
        {
            var match = Regex.Match(
                text,
                $@"{Regex.Escape(label)}\s*[:\-]?\s*(?<area>\d{{1,6}}(?:[.,]\d{{1,2}})?)\s*m(?:²|2)(?!\w)",
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));
            if (match.Success && TryParseLocalizedDecimal(match.Groups["area"].Value, out var area))
            {
                return area;
            }
        }

        return null;
    }

    private static decimal? ExtractFirstArea(string text)
    {
        var match = Regex.Match(
            text,
            @"\b(?<area>\d{1,6}(?:[.,]\d{1,2})?)\s*m(?:²|2)(?!\w)",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        return match.Success && TryParseLocalizedDecimal(match.Groups["area"].Value, out var area) ? area : null;
    }

    private static bool TryParseLocalizedDecimal(string value, out decimal result) =>
        decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static string? ExtractEnergyRating(string text)
    {
        var match = Regex.Match(
            text,
            @"\b(?:PEB|EPC)\b(?:\s*(?:label|classe|score))?\s*[:\-]?\s*(?<rating>[A-G])(?:\+{1,2}|-)?\b",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        return match.Success ? match.Groups["rating"].Value.ToUpperInvariant() : null;
    }

    private static string InferPropertyType(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower switch
        {
            _ when ContainsAny(lower, "maison de rapport", "immeuble de rapport", "immeuble à appartements", "opbrengsteigendom", "opbrengsthuis") => "IncomeBuilding",
            _ when ContainsAny(lower, "appartement", "apartment", "flat", "studio") => "Apartment",
            _ when ContainsAny(lower, "entrepot", "entrepôt", "warehouse", "magazijn") => "Warehouse",
            _ when ContainsAny(lower, "maison", "villa", "huis", "woning", "house") => "House",
            _ when ContainsAny(lower, "terrain", "grond", "land") => "Land",
            _ when ContainsAny(lower, "garage", "parking") => "Garage",
            _ when ContainsAny(lower, "bureau", "kantoor", "office") => "Office",
            _ => "Other",
        };
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static string ComputeContentHash(
        string title,
        string description,
        decimal? price,
        string externalId,
        string url)
    {
        var content = $"{title}|{description}|{price}|{externalId}|{url}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
