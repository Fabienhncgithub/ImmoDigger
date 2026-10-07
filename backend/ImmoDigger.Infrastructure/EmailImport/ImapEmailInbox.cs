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
/// Reads recent messages from a real mailbox over IMAP, using the
/// credentials in <see cref="ImapSettings"/>. It never changes flags,
/// sends, deletes, or replies to anything. Which
/// messages actually get imported (vs. skipped as already processed or
/// unrecognized) is entirely <see cref="Application.Services.EmailListingImportService"/>'s
/// call; this class's only job is "what arrived in the configured rolling
/// window". Re-reading a small window is intentional: Message-ID deduplication
/// in the database is safer than treating the mutable IMAP Seen flag as a
/// transaction checkpoint.
/// </summary>
public class ImapEmailInbox(IOptions<ImapSettings> options, ILogger<ImapEmailInbox> logger) : IEmailInbox
{
    public async Task<IReadOnlyCollection<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.Username) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException(
                "EmailImport IMAP is incomplete: Host, Username and Password are required.");
        }

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, cancellationToken);
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);

            var folderName = string.IsNullOrWhiteSpace(settings.Folder) ? "INBOX" : settings.Folder.Trim();
            var folder = folderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(folderName, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var lookbackDays = Math.Clamp(settings.LookbackDays, 1, 90);
            SearchQuery query = SearchQuery.DeliveredAfter(DateTime.UtcNow.Date.AddDays(-lookbackDays));
            if (settings.UnseenOnly)
            {
                query = query.And(SearchQuery.NotSeen);
            }

            var uids = await folder.SearchAsync(query, cancellationToken);
            var selectedUids = uids
                .OrderByDescending(uid => uid.Id)
                .Take(Math.Clamp(settings.MaxMessagesPerRun, 1, 5_000))
                .OrderBy(uid => uid.Id)
                .ToList();
            var messages = new List<EmailMessage>();

            foreach (var uid in selectedUids)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mime = await folder.GetMessageAsync(uid, cancellationToken);
                messages.Add(new EmailMessage
                {
                    MessageId = string.IsNullOrWhiteSpace(mime.MessageId) ? $"{settings.Host}/{uid}" : mime.MessageId,
                    Sender = mime.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                    Subject = mime.Subject ?? string.Empty,
                    HtmlBody = mime.HtmlBody,
                    TextBody = mime.TextBody,
                    ReceivedAt = mime.Date.UtcDateTime,
                });
            }

            await client.DisconnectAsync(true, cancellationToken);
            return messages;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is AuthenticationException or ImapProtocolException or IOException or System.Net.Sockets.SocketException)
        {
            logger.LogWarning(ex, "EmailImport: could not connect to the configured mailbox ({Host}).", settings.Host);
            // Propagate a sanitized error so ListingCollectionBackgroundService
            // records a failed run on the EmailImport source instead of a
            // misleading successful run that imported zero messages.
            throw new InvalidOperationException(
                "Impossible de lire la boîte IMAP configurée. Vérifiez l'hôte, le port, TLS et les identifiants.", ex);
        }
    }
}
