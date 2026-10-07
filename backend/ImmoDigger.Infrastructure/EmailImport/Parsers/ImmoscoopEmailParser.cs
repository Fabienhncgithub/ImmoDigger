using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>
/// Parses Immoscoop saved-search alerts. Immoscoop advertises email
/// notifications for new matching properties; no site crawling is used.
/// </summary>
public sealed partial class ImmoscoopEmailParser()
    : TemplatedAlertEmailParser("Immoscoop", "immoscoop.be", UrlPattern())
{
    // NL: /te-koop/1000-brussel/598824
    // FR: /fr/a-vendre/1000-bruxelles/598824
    [GeneratedRegex(@"immoscoop\.be/(?:fr/|en/)?(?:te-koop|a-vendre|for-sale)/[^""'\s/?#]+/(?<id>\d{5,})(?:[?/#""']|$)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
