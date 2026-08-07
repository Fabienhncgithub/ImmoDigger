using System.Text.RegularExpressions;

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
}
