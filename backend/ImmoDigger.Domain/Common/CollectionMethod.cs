namespace ImmoDigger.Domain.Common;

/// <summary>
/// How a <see cref="Entities.ListingSource"/> is allowed to be collected.
/// Deliberately explicit rather than inferred from the collector's code, so
/// the compliance decision is visible in data (seed/admin), not buried in
/// an implementation: a source whose method is <see cref="Html"/> but whose
/// <see cref="Entities.ListingSource.Allowed"/> flag is false must never run,
/// full stop.
/// </summary>
public enum CollectionMethod
{
    /// <summary>A documented or reverse-engineered-but-public JSON/REST API (e.g. Biddit's lot endpoint).</summary>
    Api,

    /// <summary>An RSS/Atom feed the source publishes for syndication.</summary>
    Rss,

    /// <summary>A public feed that isn't strictly RSS (e.g. a sitemap used for discovery).</summary>
    PublicFeed,

    /// <summary>Direct HTML parsing of a page the source serves with no anti-bot protection and no prohibition in its terms.</summary>
    Html,

    /// <summary>Listings extracted from alert emails the user already receives in their own inbox - never touches the source's site directly.</summary>
    Email,

    /// <summary>The user pastes a URL (and, if needed, the listing's details) one at a time; nothing runs unattended.</summary>
    Manual,

    /// <summary>No collection method is currently allowed for this source (explicit prohibition, active anti-bot protection, or simply not vetted yet).</summary>
    Disabled,
}
