using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>Parses Zimmo saved-search alert emails. Zimmo's own site is never scraped - see the class doc on <see cref="TemplatedAlertEmailParser"/>.</summary>
public sealed partial class ZimmoEmailParser() : TemplatedAlertEmailParser("Zimmo", "zimmo.be", UrlPattern())
{
    // Current detail ids can be short alphanumeric codes (e.g. KU4PS),
    // not only the older all-numeric ids used by the original parser.
    [GeneratedRegex(@"zimmo\.be/(?:fr|nl|en)/[^/\s""']+/(?:a-vendre|te-koop|for-sale)/[^/\s""']+/(?<id>[a-z0-9_-]{5,})/?(?:[?#""']|$)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
