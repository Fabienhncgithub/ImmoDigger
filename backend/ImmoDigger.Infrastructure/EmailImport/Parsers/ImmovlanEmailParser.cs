using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>Parses Immovlan saved-search alert emails. Immovlan's own site is never scraped - see the class doc on <see cref="TemplatedAlertEmailParser"/>.</summary>
public sealed partial class ImmovlanEmailParser() : TemplatedAlertEmailParser("Immovlan", "immovlan.be", UrlPattern())
{
    // e.g. https://www.immovlan.be/en/detail/house/for-sale/1000/brussels/rbb12345
    // Greedy middle so the id always resolves to the LAST path segment,
    // not an earlier one that happens to also satisfy the id shape (e.g.
    // a city name).
    [GeneratedRegex(@"immovlan\.be/\w{2}/detail/[^""'\s]+/(?<id>[a-z0-9]{5,})/?(?:[?""']|$)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    protected override bool TryMatchEmbeddedListing(
        HtmlNode anchor,
        HtmlNode block,
        string href,
        out string listingUrl,
        out string externalId)
    {
        listingUrl = string.Empty;
        externalId = string.Empty;

        // Only a real Immovlan property card may provide an embedded id. A
        // surrounding newsletter block can contain a generic "18 annonces"
        // link plus all cards; accepting it would assign the first card that
        // generic title and tracking-pixel image.
        var isPropertyCard = block.GetAttributeValue("class", string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains("card", StringComparer.OrdinalIgnoreCase);
        if (!isPropertyCard ||
            !Uri.TryCreate(href, UriKind.Absolute, out var tracker) ||
            tracker.Scheme != Uri.UriSchemeHttps ||
            !TrackedLinkHostPattern().IsMatch(tracker.Host) ||
            !tracker.AbsolutePath.StartsWith("/tr/cl/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var match = EmbeddedPropertyIdPattern().Match(HtmlEntity.DeEntitize(block.OuterHtml));
        if (!match.Success)
        {
            return false;
        }

        listingUrl = href;
        externalId = match.Groups["id"].Value.ToUpperInvariant();
        return true;
    }

    [GeneratedRegex(@"^r\.[a-z0-9-]+\.immovlan\.be$", RegexOptions.IgnoreCase)]
    private static partial Regex TrackedLinkHostPattern();

    [GeneratedRegex(@"api-image\.immovlan\.be/v1/property/(?<id>[a-z0-9]{5,})/", RegexOptions.IgnoreCase)]
    private static partial Regex EmbeddedPropertyIdPattern();
}
