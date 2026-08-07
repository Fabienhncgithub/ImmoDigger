using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Fetches new messages from the user's own mailbox (IMAP, a forwarding
/// webhook, ...). Deliberately the only part of the email pipeline that
/// needs real credentials - everything downstream (<see cref="IEmailListingParser"/>,
/// <see cref="IEmailListingImporter"/>) is pure parsing and can be fully
/// tested without one.
/// </summary>
public interface IEmailInbox
{
    /// <summary>
    /// Returns messages not yet processed (the caller cross-checks against
    /// <see cref="Domain.Entities.ProcessedEmailMessage"/>, so an
    /// implementation may over-return rather than track state itself).
    /// </summary>
    Task<IReadOnlyCollection<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken);
}
