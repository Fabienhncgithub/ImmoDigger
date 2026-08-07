using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Extracts listings out of a single alert email from one specific sender
/// template (Immoweb, Immovlan, Zimmo, a generic agency, ...). Never
/// touches the network - operates purely on the email content already
/// delivered to the user's own inbox, which is why this is compliant even
/// for sources whose website must never be scraped directly.
/// </summary>
public interface IEmailListingParser
{
    /// <summary>Source name this parser produces (matches a <see cref="Domain.Entities.ListingSource.Name"/>).</summary>
    string SourceName { get; }

    /// <summary>Whether this parser recognizes the sender address (e.g. an "@immoweb.be" domain).</summary>
    bool CanParse(string senderAddress);

    /// <summary>
    /// Extracts zero or more listings from the message. Returns an empty
    /// collection (never throws for content it doesn't understand) so one
    /// unparseable email never aborts the rest of the import cycle.
    /// </summary>
    IReadOnlyCollection<CollectedListing> Parse(EmailMessage message);
}
