using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmoDigger.Infrastructure.EmailImport;

/// <summary>
/// Placeholder <see cref="IEmailInbox"/>: always returns no messages. The
/// email-import pipeline (parsers, dedup, the collector wiring it into the
/// rest of the app) is fully built and tested, but actually reading a real
/// mailbox needs real credentials (IMAP host/user/password or a forwarding
/// webhook secret) that don't exist yet - inventing a working mail
/// connection without them would mean either fabricating credentials or
/// silently doing nothing while claiming otherwise. Swap this
/// registration for a real <see cref="IEmailInbox"/> (e.g. an IMAP client
/// pointed at a dedicated alerts@ mailbox) once that configuration exists.
/// </summary>
public class NullEmailInbox(ILogger<NullEmailInbox> logger) : IEmailInbox
{
    private static bool _hasWarned;

    public Task<IReadOnlyCollection<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken)
    {
        if (!_hasWarned)
        {
            _hasWarned = true;
            logger.LogWarning(
                "EmailImport: no real IEmailInbox is configured yet (NullEmailInbox is a no-op placeholder). " +
                "Set up mailbox credentials and register a real implementation to start importing alert emails.");
        }

        return Task.FromResult<IReadOnlyCollection<EmailMessage>>([]);
    }
}
