using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImmoDigger.Infrastructure.Collectors.BpostImmo;

/// <summary>
/// Collects for-sale listings from bpostimmo.be, bpost's portal for
/// surplus post-office buildings. robots.txt only disallows "/backsite";
/// no anti-bot protection was found (plain HTTP, no Cloudflare); the
/// terms of use (checked at /fr/conditions-d-utilisation) contain no
/// clause prohibiting automated access, only a general "don't harm the
/// site" clause. Volume is naturally tiny (roughly a dozen listings site-
/// wide at any time), so a single page fetch per cycle is already
/// generous, not aggressive.
///
/// The site (built on the "Zabun" real-estate platform) embeds a full
/// JSON payload for every listing directly inside the "/fr/a-vendre"
/// page via <c>app.set("estateGroups", [...])</c> - found by reading that
/// page's own HTML, not from any API documentation. No separate detail-
/// page fetch is needed at all: one page load is the entire cycle.
/// </summary>
public partial class BpostImmoCollector(IHttpClientFactory httpClientFactory, ILogger<BpostImmoCollector> logger)
    : IListingCollector
{
    public const string HttpClientName = "BpostImmo";

    private const string ListingsPath = "/fr/a-vendre";
    private const string BaseUrl = "https://bpostimmo.be";
    private const string EstateGroupsMarker = "app.set(\"estateGroups\", ";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string SourceName => "BpostImmo";

    public async Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        string html;
        try
        {
            html = await client.GetStringAsync(ListingsPath, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "BpostImmo: failed to fetch the for-sale listings page.");
            return [];
        }

        return ParseListings(html);
    }

    internal static List<CollectedListing> ParseListings(string html)
    {
        var json = ExtractBalancedArray(html, EstateGroupsMarker);
        if (json is null)
        {
            return [];
        }

        // The embedded payload is a JS object literal, not strict JSON - its
        // only deviation observed is the bare `undefined` token (used where
        // a field genuinely has no value), which JSON has no equivalent for.
        var normalized = UndefinedTokenPattern().Replace(json, ":null");

        List<BpostEstateGroupDto>? groups;
        try
        {
            groups = JsonSerializer.Deserialize<List<BpostEstateGroupDto>>(normalized, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        var results = new List<CollectedListing>();
        foreach (var wrapper in (groups ?? []).SelectMany(g => g.Estates ?? []))
        {
            var listing = MapToCollectedListing(wrapper);
            if (listing is not null)
            {
                results.Add(listing);
            }
        }

        return results;
    }

    /// <summary>Maps one estate to ImmoDigger's shape, or null if it's a rental, missing required fields, or outside the app's target area.</summary>
    internal static CollectedListing? MapToCollectedListing(BpostEstateWrapperDto wrapper)
    {
        var isForSale = wrapper.Status?.Name?.Fr?.Contains("vendre", StringComparison.OrdinalIgnoreCase) == true;
        if (!isForSale || string.IsNullOrWhiteSpace(wrapper.Id) || string.IsNullOrWhiteSpace(wrapper.Uri))
        {
            return null;
        }

        var address = wrapper.Estate?.General?.Address;
        var postalCode = address?.City?.Zip;
        if (!CollectorConstants.IsInTargetArea(postalCode))
        {
            return null;
        }

        var street = address?.Street?.Fr ?? string.Empty;
        var fullAddress = string.Join(' ', new[] { street, address?.Number }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var title = HtmlEntity.DeEntitize(wrapper.Estate?.General?.Title?.Fr) ?? "Bien immobilier";
        var description = HtmlEntity.DeEntitize(wrapper.Estate?.General?.Description?.Fr) ?? string.Empty;
        var price = wrapper.Estate?.General?.Price?.Value;
        var image = wrapper.Estate?.Pictures?.FirstOrDefault()?.File;

        return new CollectedListing
        {
            Source = "BpostImmo",
            ExternalId = wrapper.Id,
            Url = BaseUrl + wrapper.Uri,
            Title = title,
            ImageUrl = image,
            Description = description,
            Address = fullAddress,
            PostalCode = postalCode!,
            City = address?.City?.Name ?? string.Empty,
            AskingPrice = price,
            SaleType = "RegularSale",
            PropertyType = MapPropertyType(wrapper.Estate?.General?.SubType?.Fr),
            LivingArea = wrapper.Estate?.Dimensions?.AreaBuild,
            LandArea = wrapper.Estate?.Dimensions?.AreaGround,
            PebRating = ExtractPebLetter(wrapper.Estate?.Energy?.EnergyLabel),
            RawContentHash = ComputeContentHash(title, description, price, fullAddress),
        };
    }

    /// <summary>Best-effort French-keyword mapping, same approach as the other HTML-based collectors.</summary>
    private static string MapPropertyType(string? subType)
    {
        var lower = (subType ?? string.Empty).ToLowerInvariant();
        return lower switch
        {
            _ when lower.Contains("mixte") || lower.Contains("bureau") => "Office",
            _ when lower.Contains("entrepot") || lower.Contains("entrepôt") || lower.Contains("magasin") => "Warehouse",
            _ when lower.Contains("appartement") => "Apartment",
            _ when lower.Contains("maison") => "House",
            _ when lower.Contains("terrain") => "Land",
            _ => "Other",
        };
    }

    /// <summary>"epc_f" -> "F".</summary>
    private static string? ExtractPebLetter(string? energyLabel)
    {
        if (string.IsNullOrWhiteSpace(energyLabel))
        {
            return null;
        }

        var letter = energyLabel[^1..].ToUpperInvariant();
        return letter is ['A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'G'] ? letter : null;
    }

    private static string ComputeContentHash(string title, string description, decimal? price, string address)
    {
        var content = $"{title}|{description}|{price}|{address}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    /// <summary>
    /// Finds `marker` then returns the JS array literal that immediately
    /// follows it, tracking bracket depth and string boundaries (so a `[`
    /// or `]` inside a description string doesn't throw off the count) -
    /// more robust than a lazy regex, which under-/over-matches on nested
    /// arrays.
    /// </summary>
    internal static string? ExtractBalancedArray(string html, string marker)
    {
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var arrayStart = start + marker.Length;
        if (arrayStart >= html.Length || html[arrayStart] != '[')
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = arrayStart; i < html.Length; i++)
        {
            var c = html[i];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inString = true;
            }
            else if (c == '[')
            {
                depth++;
            }
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                {
                    return html[arrayStart..(i + 1)];
                }
            }
        }

        return null;
    }

    [GeneratedRegex(@":\s*undefined\b")]
    private static partial Regex UndefinedTokenPattern();
}
