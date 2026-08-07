using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>Parses Immoweb "new match" / saved-search alert emails. Immoweb's own site is never scraped - see the class doc on <see cref="TemplatedAlertEmailParser"/>.</summary>
public sealed partial class ImmowebEmailParser() : TemplatedAlertEmailParser("Immoweb", "immoweb.be", UrlPattern())
{
    // e.g. https://www.immoweb.be/en/classified/house/for-sale/brussels/1000/12345678
    [GeneratedRegex(@"immoweb\.be/\w{2}/classified/[^""'\s]*?(?<id>\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
