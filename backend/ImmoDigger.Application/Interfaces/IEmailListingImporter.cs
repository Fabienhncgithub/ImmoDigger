using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Orchestrates the email ingestion pipeline: fetch new messages from
/// <see cref="IEmailInbox"/>, skip anything already recorded as processed,
/// route each message to the <see cref="IEmailListingParser"/> that
/// recognizes its sender, and record every message as processed
/// regardless of whether it produced a listing (so an unrecognized sender
/// is never retried forever).
/// </summary>
public interface IEmailListingImporter
{
    Task<IReadOnlyCollection<CollectedListing>> ImportAsync(CancellationToken cancellationToken);
}
