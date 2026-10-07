using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>
/// Parses Spotto saved-search notifications. Spotto officially offers
/// personal search alerts; the portal itself is never crawled by ImmoDigger.
/// </summary>
public sealed partial class SpottoEmailParser()
    : TemplatedAlertEmailParser("Spotto", "spotto.be", UrlPattern())
{
    // https://www.spotto.be/nl/p/te-koop/1000-bruxelles/huis-slug/PGeFywlxTEKbfwjdgE-tKg
    [GeneratedRegex(@"spotto\.be/(?:nl|fr|en)/p/(?:te-koop|a-vendre|for-sale)/[^""'\s?#]+/(?<id>[a-z0-9_-]{8,})(?:[?/#""']|$)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
