using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImmoDigger.Infrastructure.EmailImport;

/// <summary>
/// Reads unseen messages from a real mailbox over IMAP, using the
/// credentials in <see cref="ImapSettings"/>. Only fetches and flags
/// messages as seen - never sends, deletes, or replies to anything. Which
/// messages actually get imported (vs. skipped as already processed or
/// unrecognized) is entirely <see cref="Application.Services.EmailListingImportService"/>'s
/// call; this class's only job is "what's new in the inbox right now."
/// </summary>
public class ImapEmailInbox(IOptions<ImapSettings> options, ILogger<ImapEmailInbox> logger) : IEmailInbox
{
    public async Task<IReadOnlyCollection<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, cancellationToken);
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
            var messages = new List<EmailMessage>();

            foreach (var uid in uids)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mime = await inbox.GetMessageAsync(uid, cancellationToken);
                messages.Add(new EmailMessage
                {
                    MessageId = string.IsNullOrWhiteSpace(mime.MessageId) ? $"{settings.Host}/{uid}" : mime.MessageId,
                    Sender = mime.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                    Subject = mime.Subject ?? string.Empty,
                    HtmlBody = mime.HtmlBody,
                    TextBody = mime.TextBody,
                    ReceivedAt = mime.Date.UtcDateTime,
                });

                // Marked seen once fetched so re-running a cycle doesn't
                // keep re-downloading the same message - the app's own
                // ProcessedEmailMessage table is still what actually
                // decides whether it gets imported (see
                // EmailListingImportService), this is just inbox hygiene.
                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);
            return messages;
        }
        catch (Exception ex) when (ex is AuthenticationException or ImapProtocolException or System.Net.Sockets.SocketException)
        {
            // A misconfigured/temporarily-unreachable mailbox must not take
            // the whole collection cycle down - same "log and return
            // empty" contract as every other collector's network failure.
            logger.LogWarning(ex, "EmailImport: could not connect to the configured mailbox ({Host}).", settings.Host);
            return [];
        }
    }
}
