using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>
/// Parses 2ememain.be ("Tweedehands"/Marktplaats) saved-search alert
/// emails for its real-estate ("immo") category. 2ememain's own site is
/// never scraped: its terms of use invoke the sui generis database right
/// (Code de droit economique, Livre XI) against systematic/repeated
/// extraction, with only a narrow personal-use exception tied to an RSS
/// feed - and no RSS feed for search results could be found on the live
/// site (checked robots.txt, the immo search page and their help center;
/// nothing current). Same treatment as Immoweb/Immovlan/Zimmo: alert-email
/// parsing only. Both the French 2ememain.be and Dutch 2dehands.be sender
/// and listing domains are recognized.
///
/// The URL pattern below is a best-effort guess built from 2ememain's
/// robots.txt (which disallows only the tracked "/v/.../m*?c=..." and
/// "/a/.../a*.html?c=..." variants, implying the clean "/v/.../m&lt;id&gt;"
/// and "/a/.../a&lt;id&gt;.html" forms are the canonical ad URLs) - not from a
/// real sample alert email, which wasn't available. Validate/adjust
/// against a real alert once one is available; see the class doc on
/// <see cref="TemplatedAlertEmailParser"/> for why that is expected
/// maintenance rather than a design flaw.
/// </summary>
public sealed partial class TweedehandsEmailParser()
    : TemplatedAlertEmailParser("2ememain", ["2ememain.be", "2dehands.be"], UrlPattern())
{
    [GeneratedRegex(@"(?:2ememain|2dehands)\.be/[av]/[^""'\s]+/[ma](?<id>\d{6,})(?:\.html)?", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
