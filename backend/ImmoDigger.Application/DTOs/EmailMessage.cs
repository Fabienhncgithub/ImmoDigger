namespace ImmoDigger.Application.DTOs;

/// <summary>
/// A single email fetched from the user's own inbox by an
/// <see cref="Interfaces.IEmailInbox"/>, before any listing-specific
/// parsing happens. Deliberately source-agnostic (IMAP, a forwarding
/// webhook, ...): whatever fetches the mail only needs to produce this
/// shape.
/// </summary>
public sealed record EmailMessage
{
    /// <summary>Stable identifier (IMAP Message-ID header or provider id) - the key for <see cref="Domain.Entities.ProcessedEmailMessage"/>.</summary>
    public required string MessageId { get; init; }

    public required string Sender { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string? HtmlBody { get; init; }

    public string? TextBody { get; init; }

    public DateTime ReceivedAt { get; init; }
}
