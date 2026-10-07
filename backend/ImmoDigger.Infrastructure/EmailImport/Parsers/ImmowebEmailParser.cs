using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>Parses Immoweb "new match" / saved-search alert emails. Immoweb's own site is never scraped - see the class doc on <see cref="TemplatedAlertEmailParser"/>.</summary>
public sealed partial class ImmowebEmailParser() : TemplatedAlertEmailParser("Immoweb", "immoweb.be", UrlPattern())
{
    // EN uses /classified/, FR currently uses /annonce/ and NL /zoekertje/.
    [GeneratedRegex(@"immoweb\.be/(?:fr|nl|en)/(?:annonce|zoekertje|classified)/[^""'\s]*?(?<id>\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
