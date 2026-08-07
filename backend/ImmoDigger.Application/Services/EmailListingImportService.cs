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
            }
            else
            {
                logger.LogInformation(
                    "Email import: {Count} listing(s) extracted from a {Source} alert ({Subject}).",
                    extracted.Count, parser.SourceName, message.Subject);
            }

            results.AddRange(extracted);

            await processedMessages.MarkProcessedAsync(
                new ProcessedEmailMessage
                {
                    EmailMessageId = message.MessageId,
                    Subject = message.Subject,
                    Sender = message.Sender,
                    ProcessedAt = DateTime.UtcNow,
                    ListingsExtractedCount = extracted.Count,
                },
                cancellationToken);
        }

        return results;
    }
}
