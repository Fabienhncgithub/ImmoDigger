using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>Parses Zimmo saved-search alert emails. Zimmo's own site is never scraped - see the class doc on <see cref="TemplatedAlertEmailParser"/>.</summary>
public sealed partial class ZimmoEmailParser() : TemplatedAlertEmailParser("Zimmo", "zimmo.be", UrlPattern())
{
    // e.g. https://www.zimmo.be/en/brussels-1000/for-sale/house/12345678/
    [GeneratedRegex(@"zimmo\.be/[^""'\s]*?/(?<id>\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
