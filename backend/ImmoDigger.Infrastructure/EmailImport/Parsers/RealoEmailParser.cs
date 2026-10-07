using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>
/// Parses Realo saved-search notifications. The stable property id in the
/// detail URL is used for deduplication; Realo may aggregate several agency
/// publications for the same address behind that property id.
/// </summary>
public sealed partial class RealoEmailParser()
    : TemplatedAlertEmailParser("Realo", "realo.be", UrlPattern())
{
    // https://www.realo.be/fr/avenue-de-loree-7-1000-bruxelles/69551?l=10120787
    [GeneratedRegex(@"realo\.be/(?:fr|nl|en)/[^""'\s/?#]+/(?<id>\d{4,})(?:[?/#""']|$)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
