using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ImmoDigger.Application.Services;

/// <inheritdoc cref="IEmailListingImporter" />
public class EmailListingImportService(
    IEmailInbox inbox,
    IEnumerable<IEmailListingParser> parsers,
    IProcessedEmailMessageRepository processedMessages,
    ILogger<EmailListingImportService> logger) : IEmailListingImporter
{
    public async Task<IReadOnlyCollection<CollectedListing>> ImportAsync(CancellationToken cancellationToken)
    {
        var messages = await inbox.FetchNewMessagesAsync(cancellationToken);
        var results = new List<CollectedListing>();

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await processedMessages.IsProcessedAsync(message.MessageId, cancellationToken))
            {
                continue;
            }

            var parser = parsers.FirstOrDefault(p => p.CanParse(message.Sender));
            var extracted = parser?.Parse(message) ?? [];

            if (parser is null)
            {
                logger.LogDebug("Email import: no parser recognizes sender {Sender}, skipping.", message.Sender);

                // Unknown mail (newsletters, mailbox noise, etc.) is final:
                // remember it so the rolling IMAP lookback does not inspect
                // it forever.
                await MarkProcessedAsync(message, 0, cancellationToken);
                continue;
            }

            logger.LogInformation(
                "Email import: {Count} listing(s) extracted from a {Source} alert ({Subject}).",
                extracted.Count, parser.SourceName, message.Subject);

            if (extracted.Count == 0)
            {
                // A recognized portal changing its template is recoverable:
                // do not checkpoint the message, so a parser update can
                // process it on the next run while it remains in the IMAP
                // lookback window.
                logger.LogWarning(
                    "Email import: a {Source} message was recognized but no listing URL could be parsed; " +
                    "the message remains pending for retry (Message-ID {MessageId}).",
                    parser.SourceName, message.MessageId);
                continue;
            }

            results.AddRange(extracted);
            await MarkProcessedAsync(message, extracted.Count, cancellationToken);
        }

        return results;
    }

    private Task MarkProcessedAsync(EmailMessage message, int listingsExtractedCount, CancellationToken cancellationToken) =>
        processedMessages.MarkProcessedAsync(
            new ProcessedEmailMessage
            {
                EmailMessageId = message.MessageId,
                Subject = message.Subject,
                Sender = message.Sender,
                ProcessedAt = DateTime.UtcNow,
                ListingsExtractedCount = listingsExtractedCount,
            },
            cancellationToken);
}
