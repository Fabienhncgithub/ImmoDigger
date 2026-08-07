namespace ImmoDigger.Domain.Entities;

/// <summary>
/// Records that a given inbox message has already been imported, so the
/// email import pipeline never processes the same alert twice (a mailbox
/// re-sync, an IMAP UID reset, or a manual re-run should all be idempotent).
/// </summary>
public class ProcessedEmailMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable identifier of the source email (IMAP Message-ID header or provider-specific id).</summary>
    public required string EmailMessageId { get; set; }

    public string? Subject { get; set; }

    public string? Sender { get; set; }

    public DateTime ProcessedAt { get; set; }

    /// <summary>How many listings this message produced (0 if the sender wasn't recognized or no listing could be parsed out of it).</summary>
    public int ListingsExtractedCount { get; set; }
}
