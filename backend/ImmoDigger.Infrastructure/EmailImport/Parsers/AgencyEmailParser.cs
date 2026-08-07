using System.Text.RegularExpressions;

namespace ImmoDigger.Infrastructure.EmailImport.Parsers;

/// <summary>
/// A configurable parser for a single real-estate agency's alert/newsletter
/// emails: pass its sender domain and the regex shape of its listing-detail
/// URLs. This is the "add an agency" extension point (Source 3): onboarding
/// a new agency is one <see cref="AgencyEmailParser"/> instance registered
/// in DI, not a new C# file, as long as the agency's alert emails follow
/// the same "a link to the listing, in a templated block" shape the other
/// parsers assume.
/// </summary>
public sealed class AgencyEmailParser(string sourceName, string senderDomain, Regex listingUrlPattern)
    : TemplatedAlertEmailParser(sourceName, senderDomain, listingUrlPattern);
